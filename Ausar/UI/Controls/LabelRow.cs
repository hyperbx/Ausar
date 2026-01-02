using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace Ausar.UI.Controls
{
    public class LabelRow : StackPanel
    {
        private readonly TextBlock _label;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register
        (
            nameof(Text),
            typeof(string),
            typeof(LabelRow),
            new PropertyMetadata(null)
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Style TextStyle
        {
            get => (Style)GetValue(TextStyleProperty);
            set => SetValue(TextStyleProperty, value);
        }

        public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register
        (
            nameof(TextStyle),
            typeof(Style),
            typeof(LabelRow),

            new PropertyMetadata(null, (d, e) =>
            {
                if (d is not LabelRow out_labelRow)
                    return;

                out_labelRow._label.Style = (Style)e.NewValue;
            })
        );


        public LabelRow()
        {
            Margin = new(2, 0, 0, 8);
            Orientation = Orientation.Horizontal;

            _label = new TextBlock
            {
                Margin = new(0, 1, 0, 0)
            };

            _label.SetBinding(TextBlock.TextProperty, new Binding(nameof(Text)) { Source = this });

            Children.Insert(0, _label);
        }
    }
}
