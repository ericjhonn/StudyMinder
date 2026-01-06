using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Conversor que permite valores vazios para campos int?
    /// Converte strings vazias para null em vez de gerar erro de validação
    /// </summary>
    public class NullableIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is int intValue)
                return intValue == 0 ? string.Empty : intValue.ToString();

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return null;
        }
    }
}
