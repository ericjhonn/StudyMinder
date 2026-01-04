using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string statusValues)
            {
                var values = statusValues.Split('|');
                if (values.Length == 2)
                {
                    return boolValue ? values[0] : values[1];
                }
            }
            
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
