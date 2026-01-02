using Ausar.Helpers;
using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Ausar.UI.Tabs
{
    public partial class Graphics : UserControl
    {
        private DispatcherTimer _resolutionScaleUpdateTimer;

        public Graphics()
        {
            InitializeComponent();

            _resolutionScaleUpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };

            _resolutionScaleUpdateTimer.Tick += (s, e) =>
            {
                _resolutionScaleUpdateTimer.Stop();

                if (App.GameMemory == null)
                    return;

                App.GameMemory.IsResolutionScaleUpdated = true;
            };
        }

        private void FPS_Native_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.FPS = User32Helper.GetRefreshRate();
        }

        private void ResolutionScale_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _resolutionScaleUpdateTimer?.Stop();
            _resolutionScaleUpdateTimer?.Start();
        }
    }
}
