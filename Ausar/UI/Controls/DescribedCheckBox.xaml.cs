using System.Windows;
using System.Windows.Controls;

namespace Ausar.UI.Controls
{
    public partial class DescribedCheckBox : UserControl
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register
        (
            nameof(Content),
            typeof(string),
            typeof(DescribedCheckBox),
            new PropertyMetadata(nameof(Content))
        );

        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register
        (
            nameof(Description),
            typeof(string),
            typeof(DescribedCheckBox),
            new PropertyMetadata(string.Empty)
        );

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register
        (
            nameof(IsChecked),
            typeof(bool),
            typeof(DescribedCheckBox),
            new PropertyMetadata(false)
        );

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public DescribedCheckBox()
        {
            InitializeComponent();
        }
    }
}
