using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyMinder.Converters
{
    public class SimpleBoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Usar cores do tema: PrimaryBrush para estudos, TextSecondaryBrush para sem estudos
                return boolValue ? "#FF6B4FFF" : "#FFB0B0B0";
            }
            return "#FFB0B0B0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
