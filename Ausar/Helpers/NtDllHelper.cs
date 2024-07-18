using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Ausar.Interop
{
    public partial class NtDllHelper
    {
        public const uint IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B;
        public const uint IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B;

        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial int NtQueryInformationThread(nint in_threadHandle, int in_threadInformationClass, nint in_threadInformation, int in_threadInformationLength, nint in_returnLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DATA_DIRECTORY
        {
            public int VirtualAddress;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfNameOrdinals;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 48)]
        public class THREAD_BASIC_INFORMATION
        {
            public long ExitStatus;
            public nint TebBaseAddress;
            public long ProcessID;
            public long ThreadID;
            public long AffinityMask;
            public int Priority;
            public int BasePriority;
        }

        public static THREAD_BASIC_INFORMATION GetThreadInformation(Kernel32.SafeHTHREAD? in_handle)
        {
            var threadInfo = new THREAD_BASIC_INFORMATION();
            var threadInfoSize = Marshal.SizeOf<THREAD_BASIC_INFORMATION>();

            var pThreadInfo = Marshal.AllocHGlobal(threadInfoSize);

            NtQueryInformationThread(in_handle.DangerousGetHandle(), 0, pThreadInfo, threadInfoSize, 0);

            Marshal.PtrToStructure(pThreadInfo, threadInfo);
            Marshal.FreeHGlobal(pThreadInfo);

            return threadInfo;
        }
    }
}
