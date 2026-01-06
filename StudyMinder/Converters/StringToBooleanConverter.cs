using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Converte duas strings para booleano comparando se são iguais.
    /// Usado para verificar se um botão de filtro está ativo via MultiBinding.
    /// </summary>
    public class StringToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string filtroAtivo && values[1] is string commandParameter)
            {
                return filtroAtivo.Equals(commandParameter, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Versão legada para compatibilidade com IValueConverter.
    /// </summary>
    public class StringToBooleanConverterLegacy : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string valueString && parameter is string parameterString)
            {
                return valueString.Equals(parameterString, StringComparison.OrdinalIgnoreCase);
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
