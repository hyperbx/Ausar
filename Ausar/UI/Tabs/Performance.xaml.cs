using Ausar.Enums;
using System.Windows.Controls;

namespace Ausar.UI.Tabs
{
    public partial class Performance : UserControl
    {
        public Performance()
        {
            InitializeComponent();
        }

        private void Performance_Preset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PerformanceExpander.IsExpanded = (EPerformancePreset)PerformancePresetField.SelectedIndex == EPerformancePreset.Custom;
        }
    }
}
