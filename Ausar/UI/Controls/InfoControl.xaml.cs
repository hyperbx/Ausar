using Ausar.Services;
using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ausar.UI.Controls
{
    public partial class InfoControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register
        (
            nameof(Title),
            typeof(string),
            typeof(InfoControl),
            new PropertyMetadata(null)
        );

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register
        (
            nameof(Description),
            typeof(string),
            typeof(InfoControl),
            new PropertyMetadata(null)
        );

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty IsWarningProperty = DependencyProperty.Register
        (
            nameof(IsWarning),
            typeof(bool),
            typeof(InfoControl),
            new PropertyMetadata(false)
        );

        public bool IsWarning
        {
            get => (bool)GetValue(IsWarningProperty);
            set => SetValue(IsWarningProperty, value);
        }

        public InfoControl()
        {
            InitializeComponent();

            Loaded += PatchInfoControl_Loaded;
        }

        private void PatchInfoControl_Loaded(object sender, RoutedEventArgs e)
        {
            IconControl.Margin = new Thickness(IsWarning ? 5 : 10, 0, 0, 0);
            IconControl.Text = IsWarning ? "warning" : "info";
            IconControl.Foreground = IsWarning ? Brushes.Yellow : Brushes.SteelBlue;
            IconControl.InvalidateVisual();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var lines = Description.Split("\\n");

            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimStart();

            new ContentDialog()
            {
                Title = Title,
                Content = string.Join('\n', lines),
                PrimaryButtonText = LocaleService.Localise("Common_OK")
            }
            .ShowAsync();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!Description.Contains("\\n"))
            {
                ToolTip = Description;
                return;
            }

            if (IsWarning)
            {
                ToolTip = LocaleService.Localise("InfoControl_ClickForMoreInfo_Experimental");
            }
            else
            {
                ToolTip = LocaleService.Localise("InfoControl_ClickForMoreInfo");
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (ToolTip is not ToolTip out_tt)
                return;

            out_tt.IsOpen = false;
        }
    }
}
