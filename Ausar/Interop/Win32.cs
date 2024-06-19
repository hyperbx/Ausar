using System.Runtime.InteropServices;

namespace Ausar.Interop
{
    public partial class Win32
    {
        public const int MEM_COMMIT = 0x1000;
        public const int PAGE_EXECUTE_READWRITE = 0x40;
        public const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const int THREAD_ALL_ACCESS = 0x1F03FF;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(nint in_hObject);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWow64Process(nint in_hProcess, [MarshalAs(UnmanagedType.Bool)] out bool out_wow64Process);

        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial int NtQueryInformationThread(nint in_threadHandle, int in_threadInformationClass, nint in_threadInformation, int in_threadInformationLength, nint in_returnLength);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint OpenThread(int in_dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool in_bInheritHandle, int in_dwThreadId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReadProcessMemory(nint in_hProcess, nint in_lpBaseAddress, byte[] in_lpBuffer, nint in_dwSize, out nint out_lpNumberOfBytesRead);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint VirtualAllocEx(nint in_hProcess, nint in_lpAddress, nint in_dwSize, uint in_flAllocationType, uint in_flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VirtualProtectEx(nint in_hProcess, nint in_lpAddress, nint in_dwSize, uint in_flNewProtect, out uint out_lpflOldProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WriteProcessMemory(nint in_hProcess, nint in_lpBaseAddress, byte[] in_lpBuffer, uint in_dwSize, out int in_lpNumberOfBytesWritten);

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

        public static THREAD_BASIC_INFORMATION GetThreadInformation(nint in_handle)
        {
            var threadInfo = new THREAD_BASIC_INFORMATION();
            var threadInfoSize = Marshal.SizeOf<THREAD_BASIC_INFORMATION>();

            var pThreadInfo = Marshal.AllocHGlobal(threadInfoSize);

            NtQueryInformationThread(in_handle, 0, pThreadInfo, threadInfoSize, 0);

            Marshal.PtrToStructure(pThreadInfo, threadInfo);
            Marshal.FreeHGlobal(pThreadInfo);

            return threadInfo;
        }
    }
}
