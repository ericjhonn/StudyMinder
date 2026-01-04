using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class DoubleToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Garante que o valor esteja entre 0 e 100
                double percentage = Math.Max(0, Math.Min(100, doubleValue));
                // Retorna o valor da porcentagem para uso direto
                return percentage;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
