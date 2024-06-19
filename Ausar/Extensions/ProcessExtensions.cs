using Ausar.Interop;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ausar.Extensions
{
    public static class ProcessExtensions
    {
        public static bool Is64Bit(this Process in_process)
        {
            if (!Win32.IsWow64Process(in_process.Handle, out var isWoW64))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return !isWoW64;
        }
    }
}
