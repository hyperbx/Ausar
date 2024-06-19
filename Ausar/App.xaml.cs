using Ausar.Game;
using Ausar.Helpers;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Ausar
{
    public partial class App : Application
    {
        public const string GameProcessName = "halo5forge";

        public static Configuration Settings { get; private set; } = new Configuration().Import();

        public static Memory GameMemory { get; set; }

        public static bool IsUIDebug { get; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show($"An unhandled exception has occurred.\n\n" +
                    $"{(e.ExceptionObject as Exception).ToString().Truncate(500, true)}",
                    "Ausar", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(-1);
            };
#endif
            base.OnStartup(e);
        }

        public static void Restart()
        {
            var path = Environment.ProcessPath;

            if (!File.Exists(path))
                throw new FileNotFoundException($"Failed to restart application as the executable is missing.\n\nExpected path: {path}");

            Process.Start(path);

            Environment.Exit(0);
        }
    }
}
