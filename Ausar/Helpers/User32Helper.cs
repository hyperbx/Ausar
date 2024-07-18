using Ausar.Enums;
using Vanara.PInvoke;

namespace Ausar.Helpers
{
    internal partial class User32Helper
    {
        public static int GetRefreshRate()
        {
            var devMode = new DEVMODE();

            if (User32.EnumDisplaySettings(null, User32.ENUM_CURRENT_SETTINGS, ref devMode))
                return (int)devMode.dmDisplayFrequency;

            return 0;
        }

        public static bool IsKeyDown(EKeys in_key)
        {
            return (User32.GetAsyncKeyState((int)in_key) & 0x8000) != 0;
        }
    }
}
