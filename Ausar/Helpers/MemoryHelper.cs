using System.Runtime.InteropServices;

namespace Ausar.Helpers
{
    internal class MemoryHelper
    {
        public static T ByteArrayToUnmanagedType<T>(byte[] in_data) where T : unmanaged
        {
            if (in_data == null || in_data.Length <= 0)
                return default;

            var handle = GCHandle.Alloc(in_data, GCHandleType.Pinned);

            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] UnmanagedTypeToByteArray<T>(T in_structure) where T : unmanaged
        {
            byte[] data = new byte[Marshal.SizeOf(typeof(T))];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                Marshal.StructureToPtr(in_structure, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            return data;
        }
    }
}
