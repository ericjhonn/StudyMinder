using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyMinder.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
            {
                try
                {
                    // Tenta converter a string hexadecimal para um Brush
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // Se falhar, retorna uma cor padrão
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db"));
                }
            }
            
            // Cor padrão se não houver valor
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
