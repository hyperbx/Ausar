using Ausar.Extensions;
using Ausar.Helpers;
using Ausar.Interop;
using Ausar.Logger;
using Ausar.Services.Enums;
using Gee.External.Capstone.X86;
using Keystone;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Ausar.Services
{
    public class ProcessMemoryService(Process in_process)
    {
        private Dictionary<nint, byte[]> _preservedMemory = [];

        public Process Process { get; } = in_process;

        public nint ASLR(long in_address)
        {
            return (nint)(Process.MainModule.BaseAddress + (in_address - (Process.Is64Bit() ? 0x140000000 : 0x400000)));
        }

        public bool IsAccessible(nint in_address)
        {
            var buffer = new byte[1];

            return Win32.ReadProcessMemory(Process.Handle, in_address, buffer, 1, out _);
        }

        public nint Alloc(int in_size)
        {
            var result = Win32.VirtualAllocEx(Process.Handle, 0, in_size, Win32.MEM_COMMIT, Win32.PAGE_EXECUTE_READWRITE);

            if (result == 0)
                throw new Win32Exception($"Allocation failed ({Marshal.GetLastWin32Error()}).");

            return result;
        }

        public byte[] ReadBytes(nint in_address, int in_length)
        {
            var result = new byte[in_length];

            if (in_address == 0)
                return [];

            if (!Win32.ReadProcessMemory(Process.Handle, in_address, result, in_length, out _))
                throw new Exception($"Failed to read memory at 0x{in_address:X} in process {Process.Id} ({Marshal.GetLastWin32Error()}).");

            return result;
        }

        public void WriteBytes(nint in_address, byte[] in_data, bool in_isProtected = false, bool in_isPreserved = false)
        {
            var oldProtect = 0U;

            if (in_isPreserved)
                Preserve(in_address, in_data.Length);

            if (in_isProtected)
                Win32.VirtualProtectEx(Process.Handle, in_address, in_data.Length, Win32.PAGE_EXECUTE_READWRITE, out oldProtect);

            if (!Win32.WriteProcessMemory(Process.Handle, in_address, in_data, (uint)in_data.Length, out _))
                throw new Exception($"Failed to write memory at 0x{in_address:X} in process {Process.Id} ({Marshal.GetLastWin32Error()}).");

            if (in_isProtected)
                Win32.VirtualProtectEx(Process.Handle, in_address, in_data.Length, oldProtect, out _);
        }

        public void WriteProtectedBytes(nint in_address, byte[] in_data, bool in_isPreserved = false)
        {
            WriteBytes(in_address, in_data, true, in_isPreserved);
        }

        public T Read<T>(nint in_address) where T : unmanaged
        {
            var data = ReadBytes(in_address, Marshal.SizeOf<T>());

            if (data.Length <= 0)
                return default;

            return MemoryHelper.ByteArrayToUnmanagedType<T>(data);
        }

        public void Write<T>(nint in_address, T in_data, bool in_isProtected = false, bool in_isPreserved = false) where T : unmanaged
        {
            var data = MemoryHelper.UnmanagedTypeToByteArray(in_data);

            if (data.Length <= 0)
                return;

            if (in_isProtected)
            {
                WriteProtectedBytes(in_address, data, in_isPreserved);
            }
            else
            {
                WriteBytes(in_address, data, in_isPreserved);
            }
        }

        public void WriteProtected<T>(nint in_address, T in_data, bool in_isPreserved = false) where T : unmanaged
        {
            Write(in_address, in_data, true, in_isPreserved);
        }

        public void WriteNop(nint in_address, int in_count = 1, bool in_isPreserved = true)
        {
            if (in_isPreserved)
                Preserve(in_address, in_count);

            for (int i = 0; i < in_count; i++)
                WriteProtected<byte>(in_address + i, 0x90);
        }

        public void WriteAsmHook(string in_code, nint in_address, EHookParameter in_parameter = EHookParameter.Jump, bool in_isPreserved = true)
        {
            var is64Bit = Process.Is64Bit();
            var minHookLength = is64Bit ? 14 : 5;
            var returnAddr = in_address;

            using (var disassembler = new CapstoneX86Disassembler(X86DisassembleMode.LittleEndian | (is64Bit ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32)))
            {
                var buffer = ReadBytes(in_address, 64);
                var instrs = disassembler.Disassemble(buffer);
            
                var hookLength = 0;
            
                foreach (var instr in instrs)
                {
                    hookLength += instr.Bytes.Length;
            
                    if (hookLength >= minHookLength)
                        break;
                }

                if (in_isPreserved)
                    Preserve(in_address, hookLength);
            
                // Kill existing instructions to safely inject our hook.
                for (nint i = in_address; i < in_address + hookLength; i++)
                    WriteNop(i);
            
                returnAddr += hookLength;
            }

            using (var assembler = new Engine(Keystone.Architecture.X86, is64Bit ? Mode.X64 : Mode.X32))
            {
                var buffer = assembler.Assemble(in_code, (ulong)in_address);
                var length = buffer.Buffer.Length + minHookLength;
                var memPtr = Alloc(length);
#if DEBUG
                LoggerService.Utility($"Writing mid-asm hook code to 0x{memPtr:X} in process {Process.Id}...");
#endif
                // Write code buffer to our allocated memory in the attached process.
                WriteBytes(memPtr, buffer.Buffer);

                // Write return instruction.
                if (in_parameter == EHookParameter.Jump)
                {
                    var retBuffer = new byte[minHookLength];
                    var retAddrBuffer = MemoryHelper.UnmanagedTypeToByteArray(returnAddr);

                    if (is64Bit)
                    {
                        retBuffer[0] = 0xFF;
                        retBuffer[1] = 0x25;

                        Array.Copy(retAddrBuffer, 0, retBuffer, 6, 8);
                    }
                    else
                    {
                        retBuffer[0] = 0xE9;

                        Array.Copy(retAddrBuffer, 0, retBuffer, 1, 4);
                    }

                    WriteBytes(memPtr + buffer.Buffer.Length, retBuffer);
                }
                else
                {
                    Write<byte>(memPtr + buffer.Buffer.Length, 0xC3);
                }

                // Write jump instruction.
                {
                    var jmpBuffer = new byte[minHookLength];
                    var jmpAddrBuffer = MemoryHelper.UnmanagedTypeToByteArray(memPtr);

                    if (is64Bit)
                    {
                        jmpBuffer[0] = 0xFF;
                        jmpBuffer[1] = (byte)(in_parameter == EHookParameter.Jump ? 0x25 : 0x15);

                        Array.Copy(jmpAddrBuffer, 0, jmpBuffer, 6, 8);
                    }
                    else
                    {
                        jmpBuffer[0] = (byte)(in_parameter == EHookParameter.Jump ? 0xE9 : 0xE8);

                        Array.Copy(jmpAddrBuffer, 0, jmpBuffer, 1, 4);
                    }

                    WriteBytes(in_address, jmpBuffer);
                }
            }
        }

        public string ReadStringNullTerminated(nint in_address, Encoding in_encoding = null)
        {
            var data = new List<byte>();
            var encoding = in_encoding ?? Encoding.UTF8;

            var addr = in_address;

            if (encoding == Encoding.Unicode ||
                encoding == Encoding.BigEndianUnicode)
            {
                ushort us;

                while ((us = Read<ushort>(addr)) != 0)
                {
                    data.Add((byte)(us & 0xFF));
                    data.Add((byte)((us >> 8) & 0xFF));
                    addr += 2;
                }
            }
            else
            {
                byte b;

                while ((b = Read<byte>(addr)) != 0)
                {
                    data.Add(b);
                    addr++;
                }
            }

            return encoding.GetString(data.ToArray());
        }

        public void Preserve(nint in_address, int in_length, bool in_isPreservedOnce = true)
        {
            if (in_address == 0)
                return;

            if (_preservedMemory.ContainsKey(in_address))
            {
                if (in_isPreservedOnce)
                    return;

                _preservedMemory.Remove(in_address);
            }

            _preservedMemory.Add(in_address, ReadBytes(in_address, in_length));
        }

        public void Restore(nint in_address)
        {
            if (in_address == 0)
                return;

            if (!_preservedMemory.ContainsKey(in_address))
                return;

            WriteProtectedBytes(in_address, _preservedMemory[in_address]);
        }
    }
}
