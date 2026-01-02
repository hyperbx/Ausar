using Ausar.Game;
using Ausar.Helpers;
using Ausar.Logger.Handlers;
using Ausar.Services;
using ModernWpf.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Ausar
{
    public partial class Trainer : Window
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public Trainer()
        {
            InitializeComponent();

            DebugLog.ItemsSource = FrontendLogger.Logs;

            AusarVersionText.Text = LocaleService.Localise("Common_Version", AssemblyHelper.GetInformationalVersion());
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Task.Run(() => WaitForProcess(_cancellationTokenSource.Token));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!App.Settings.IsCrosshairScaleModeAvailable ||
                !App.Settings.IsApplyCustomFOVToViewModelAvailable ||
                !App.Settings.IsDynamicAspectRatioAvailable)
            {
                var result = MessageBox.Show
                (
                    LocaleService.Localise("Message_Warning_ExitWithHooksInstalled_Body"),
                    "Ausar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }

        private void Status(string in_text, Brush in_colour = null)
        {
            StatusText.Text = in_text;

            if (in_colour == null)
                return;

            StatusBar.Background = in_colour;
        }

        private async Task WaitForProcess(CancellationToken in_cancellationToken)
        {
            while (!in_cancellationToken.IsCancellationRequested)
            {
                if (ProcessHelper.TryGetProcessByName(App.GameProcessName, out var out_process))
                {
                    Dispatcher.BeginInvoke(() => OnProcessFound(out_process));
                }
                else
                {
                    Dispatcher.BeginInvoke(() => OnProcessWait());
                }

                await Task.Delay(2500, in_cancellationToken);
            }
        }

        private async void OnProcessFound(Process in_process)
        {
#if !DEBUG
            try
#endif
            {
                var isPatchingEnabled = App.IsFrontendDebug
                    ? DisableRuntimePatcherCheckBox.IsChecked == false
                    : true;

                if (isPatchingEnabled)
                {
                    if (App.GameMemory == null)
                    {
                        var version = Information.GetVersionString(in_process);

                        if (version != Information.ExpectedVersion)
                            throw new NotSupportedException(LocaleService.Localise("Message_Error_GameVersionNotSupported_Body", Information.ExpectedVersion, version));

                        HaloUWPVersionText.Text = version;

                        App.GameMemory = new(in_process);
                    }
                }
                else
                {
                    OnProcessWait();
                }

                Status(LocaleService.Localise("Status_GameIsRunning", in_process.Id), Brushes.DarkGreen);
            }
#if !DEBUG
            catch (Exception out_ex)
            {
                _cancellationTokenSource.Cancel();

                var dialog = new ContentDialog()
                {
                    Title = LocaleService.Localise("Message_Error_Generic_Title"),
                    Content = out_ex.Message,
                    PrimaryButtonText = LocaleService.Localise("Common_OK")
                };
                
                await dialog.ShowAsync();

                Environment.Exit(-1);
            }
#endif
        }

        private void OnProcessWait()
        {
            App.Settings.IsCrosshairScaleModeAvailable = true;
            App.Settings.IsApplyCustomFOVToViewModelAvailable = true;
            App.Settings.IsDynamicAspectRatioAvailable = true;
            App.Settings.ResolutionString = string.Empty;

            App.GameMemory?.Dispose();
            App.GameMemory = null;

            Status(LocaleService.Localise("Status_WaitingForGame"), Brushes.DarkRed);
        }

        private void Settings_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Ausar.Language.UpdateResources();
        }

        private async void Settings_ClearSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = await new ContentDialog()
            {
                Title = LocaleService.Localise("Settings_General_RestoreDefaults"),
                Content = LocaleService.Localise("Message_Question_RestoreDefaults_Body"),
                PrimaryButtonText = LocaleService.Localise("Common_Yes"),
                SecondaryButtonText = LocaleService.Localise("Common_No")
            }
            .ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

#if !DEBUG
            try
#endif
            {
                App.Settings.Reset();
            }
#if !DEBUG
            catch (Exception out_ex)
            {
                await new ContentDialog()
                {
                    Title = LocaleService.Localise("Message_Error_Generic_Title"),
                    Content = LocaleService.Localise("Message_Error_FailedToResetConfig_Body", out_ex),
                    PrimaryButtonText = LocaleService.Localise("Common_OK")
                }
                .ShowAsync();
            }
#endif
        }

        private void About_Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessHelper.StartWithDefaultProgram(e.Uri.ToString());
        }

        private void About_GitHub_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartWithDefaultProgram("https://github.com/hyperbx/Ausar");
        }
    }
}