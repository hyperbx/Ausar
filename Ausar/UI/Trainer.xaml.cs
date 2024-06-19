using Ausar.Enums;
using Ausar.Game;
using Ausar.Helpers;
using Ausar.Interop;
using Ausar.Logger.Handlers;
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

        private DispatcherTimer _resolutionScaleUpdateTimer;

        public Trainer()
        {
            InitializeComponent();

            FPSHyperlinkHint.Text = string.Format(FPSHyperlinkHint.Text, User32.GetRefreshRate());

            _resolutionScaleUpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
            _resolutionScaleUpdateTimer.Tick += (s, e) =>
            {
                _resolutionScaleUpdateTimer.Stop();

                if (App.GameMemory == null)
                    return;

                App.GameMemory.IsResolutionScaleUpdated = true;
            };

            DebugLog.ItemsSource = FrontendLogger.Logs;

            AusarVersionText.Text = $"Version {AssemblyHelper.GetInformationalVersion()}";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Task.Run(() => WaitForProcess(_cancellationTokenSource.Token));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!App.Settings.IsUninstallPatchesOnExit)
                return;

            App.GameMemory?.UninstallPatches();
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
            {
#endif
                var isPatchingEnabled = App.IsFrontendDebug
                    ? DisableRuntimePatcherCheckBox.IsChecked == false
                    : true;

                if (isPatchingEnabled)
                {
                    if (App.GameMemory == null)
                    {
                        var version = Information.GetVersionString(in_process);

                        if (version != Information.ExpectedVersion)
                            throw new NotSupportedException($"The installed version of Halo 5: Forge is not supported.\n\nExpected: {Information.ExpectedVersion}\nReceived: {version}");

                        HaloUWPVersionText.Text = version;

                        App.GameMemory = new(in_process);
                    }
                }
                else
                {
                    OnProcessWait();
                }

                Status($"Halo 5: Forge is running (PID {in_process.Id})...", Brushes.DarkGreen);
#if !DEBUG
            }
            catch (Exception out_ex)
            {
                _cancellationTokenSource.Cancel();

                var dialog = new ContentDialog()
                {
                    Title = "An exception has occurred",
                    Content = out_ex.Message,
                    PrimaryButtonText = "OK"
                };
                
                await dialog.ShowAsync();

                Environment.Exit(-1);
            }
#endif
        }

        private void OnProcessWait()
        {
            App.Settings.IsDynamicAspectRatioAvailable = true;
            App.Settings.ResolutionString = string.Empty;

            App.GameMemory?.Dispose();
            App.GameMemory = null;

            Status("Waiting for Halo 5: Forge...", Brushes.DarkRed);
        }

        private void Tweaks_FPS_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.FPS = User32.GetRefreshRate();
        }

        private void Tweaks_DynamicAspectRatio_Experimental_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var message =
            """
            This feature is experimental and may cause instability if used improperly.

            Ausar must always be running to update the game's aspect ratio at runtime, and you must be at the main menu in order for the changes to apply.

            Known issues;
            • Resizing the game window too many times will cause UI elements and the font renderer to start artefacting until they eventually disappear (which is why being at the main menu is a requirement for now).
            • UI elements rendered in 3D space (such as navigation points) are still drawn at 16:9 and may appear stretched at non-16:9 resolutions.
            • Ludicrously wide aspect ratios may have graphical errors with certain buffers being drawn at the incorrect offset.
            """;

            new ContentDialog()
            {
                Title = "Dynamic Aspect Ratio",
                Content = message,
                PrimaryButtonText = "OK"
            }
            .ShowAsync();
        }

        private void Tweaks_ResolutionScale_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _resolutionScaleUpdateTimer?.Stop();
            _resolutionScaleUpdateTimer?.Start();
        }

        private void Tweaks_PerformancePreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PerformanceExpander.IsExpanded = (EPerformancePreset)PerformancePresetField.SelectedIndex == EPerformancePreset.Custom;
        }

        private async void Settings_ClearSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = await new ContentDialog()
            {
                Title = "Restore Defaults",
                Content = "Are you sure you want to reset your configuration?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            }
            .ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

#if !DEBUG
            try
            {
#endif
                App.Settings.Reset();
#if !DEBUG
            }
            catch (Exception out_ex)
            {
                await new ContentDialog()
                {
                    Title = "An exception has occurred",
                    Content = $"Failed to reset configuration.\n\n{out_ex}",
                    PrimaryButtonText = "OK"
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