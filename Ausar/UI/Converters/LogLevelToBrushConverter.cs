using Ausar.Logger.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ausar.UI.Converters
{
    internal class LogLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ELogLevel logLevel)
            {
                return logLevel switch
                {
                    ELogLevel.Utility => Brushes.Green,
                    ELogLevel.Warning => Brushes.Yellow,
                    ELogLevel.Error => Brushes.Red,
                    _ => Brushes.White
                };
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
