using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Converter simples para cores do heatmap
    /// True = roxo (#FF6B4FFF), False = cinza (#FFB0B0B0)
    /// </summary>
    public class HeatmapColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _corComEstudo = new SolidColorBrush(Color.FromArgb(0xFF, 0x6B, 0x4F, 0xFF));
        private static readonly SolidColorBrush _corSemEstudo = new SolidColorBrush(Color.FromArgb(0xFF, 0xB0, 0xB0, 0xB0));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? _corComEstudo : _corSemEstudo;
            }
            
            return _corSemEstudo;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
