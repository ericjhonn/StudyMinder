using System;
using System.Globalization;
using System.Windows.Data;
using StudyMinder.Models;

namespace StudyMinder.Converters
{
    public class TipoEventoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                Estudo _ => "Estudo",
                Revisao _ => "Revisão",
                EditalCronograma _ => "Evento do Edital",
                _ => "Desconhecido"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
