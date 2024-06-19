using System.Diagnostics;

namespace Ausar.Helpers
{
    internal class ProcessHelper
    {
        public static void StartWithDefaultProgram(string in_url, string in_args = "")
        {
            Process.Start
            (
                new ProcessStartInfo("cmd", $"/c start \"\" \"{in_url}\" {in_args}")
                {
                    CreateNoWindow = true
                }
            );
        }

        public static bool TryGetProcessByName(string in_name, out Process out_process)
        {
            var processes = Process.GetProcessesByName(in_name);

            if (processes.Length <= 0)
            {
                out_process = null;
                return false;
            }

            out_process = processes.FirstOrDefault();
            return true;
        }
    }
}
