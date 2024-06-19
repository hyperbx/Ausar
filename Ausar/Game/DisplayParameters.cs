using System.Runtime.InteropServices;

namespace Ausar.Game
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DisplayParameters
    {
        [FieldOffset(0x00)] public int InternalWidth;
        [FieldOffset(0x04)] public int InternalHeight;
        [FieldOffset(0x08)] public bool Refresh;
        [FieldOffset(0x0C)] public int StoredWidth;
        [FieldOffset(0x10)] public int StoredHeight;
        [FieldOffset(0x14)] public int WindowWidth;
        [FieldOffset(0x18)] public int WindowHeight;
        [FieldOffset(0x1C)] public int WindowWidthOld;
        [FieldOffset(0x20)] public int WindowHeightOld;
        [FieldOffset(0x24)] public int ScreenWidth;
        [FieldOffset(0x28)] public int ScreenHeight;
        [FieldOffset(0x38)] public int UIWidth;
        [FieldOffset(0x3C)] public int UIHeight;
    }
}
