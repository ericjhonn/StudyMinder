using System;
using System.Globalization;
using System.Windows.Data;
using StudyMinder.Utils;

namespace StudyMinder.Converters
{
    public class PeriodoMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not DateTime data || values[1] is not bool isSemanal)
            {
                return string.Empty;
            }

            if (isSemanal)
            {
                var inicioSemana = DateUtils.GetInicioSemana(data);
                var fimSemana = inicioSemana.AddDays(6);
                return $"{inicioSemana:dd/MM} - {fimSemana:dd/MM/yyyy}";
            }
            else
            {
                return culture.TextInfo.ToTitleCase(data.ToString("MMMM yyyy", culture));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
