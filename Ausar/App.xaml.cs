using Ausar.Extensions;
using Ausar.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Ausar
{
    public partial class App : Application
    {
        public const string GameProcessName = "halo5forge";

        public static Configuration Settings { get; private set; } = new Configuration().Import();

        public static Languages? SupportedLanguages { get; set; }

        public static Language? CurrentLanguage { get; set; }

        public static Game.Memory? GameMemory { get; set; }

        public static bool IsFrontendDebug { get; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show
                (
                    LocaleService.Localise("Message_Error_GenericUnhandled_Title", (e.ExceptionObject as Exception).ToString().Truncate(500, true)),
                    "Ausar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Environment.Exit(-1);
            };
#endif

            Language.LoadResources();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (!Settings.IsUninstallPatchesOnExit)
                return;

            GameMemory?.UninstallPatches();

            base.OnExit(e);
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
