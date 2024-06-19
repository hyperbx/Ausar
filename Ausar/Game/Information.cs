using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ausar.Game
{
    internal class Information
    {
        private static readonly Regex _versionRegex = new(@"Microsoft.Halo5Forge_([0-9]+.[0-9]+.[0-9]+.[0-9]+).");

        public const string ExpectedVersion = "1.194.6192.2";

        public static string GetVersionString(Process in_process)
        {
            var @default = "0.0.0.0";

            if (in_process.HasExited)
                return @default;

            var match = _versionRegex.Match(in_process.MainModule.FileName);

            if (!match.Success)
                return @default;

            return match.Groups[1].Value;
        }
    }
}
