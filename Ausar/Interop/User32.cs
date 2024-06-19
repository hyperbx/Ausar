using Ausar.Enums;
using System.Runtime.InteropServices;

namespace Ausar.Interop
{
    internal partial class User32
    {
        private const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplaySettings(string in_lpszDeviceName, int in_iModeNum, ref DEVMODE in_lpDevMode);

        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial short GetAsyncKeyState(EKeys in_vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        public static int GetRefreshRate()
        {
            var devMode = new DEVMODE();

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
                return devMode.dmDisplayFrequency;

            return 0;
        }

        public static bool IsKeyDown(EKeys in_key)
        {
            return (GetAsyncKeyState(in_key) & 0x8000) != 0;
        }
    }
}
