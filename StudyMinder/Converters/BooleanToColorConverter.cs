using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyMinder.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorParts = colors.Split('|');
                if (colorParts.Length == 2)
                {
                    var trueColor = colorParts[0].Trim();
                    var falseColor = colorParts[1].Trim();
                    
                    return boolValue 
                        ? (object)new SolidColorBrush((Color)ColorConverter.ConvertFromString(trueColor))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString(falseColor));
                }
            }
            
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
