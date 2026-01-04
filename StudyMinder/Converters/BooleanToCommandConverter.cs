using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class BooleanToCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return DependencyProperty.UnsetValue;

            if (parameter is not string styleKeys)
                return DependencyProperty.UnsetValue;

            var keys = styleKeys.Split('|');
            if (keys.Length != 2)
                return DependencyProperty.UnsetValue;

            var styleKey = boolValue ? keys[0] : keys[1];
            
            // This assumes a naming convention, e.g., "Modern" becomes "ModernButtonStyle"
            // This is brittle, but matches the apparent intent
            var fullStyleKey = $"{styleKey}ButtonStyle"; 
            
            return Application.Current.TryFindResource(fullStyleKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
