using System;
using System.Globalization;
using System.Windows.Data;
using StudyMinder.Models;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Conversor para verificar se um item está selecionado em uma coleção.
    /// </summary>
    public class IsItemSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is System.Collections.IList collection && values[1] is Assunto assunto)
            {
                return collection.Contains(assunto);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para transformar boolean em ícone.
    /// </summary>
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool mostrandoAtivos)
            {
                return mostrandoAtivos ? "CheckCircle" : "PlusCircle";
            }
            return "BookOpenVariant";
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para verificar se página não é a primeira.
    /// </summary>
    public class NotEqualToFirstPageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pagina)
            {
                return pagina > 1;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para verificar se página não é a última.
    /// </summary>
    public class NotEqualToLastPageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pagina && parameter is int totalPaginas)
            {
                return pagina < totalPaginas;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
