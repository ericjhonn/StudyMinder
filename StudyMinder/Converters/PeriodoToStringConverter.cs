using StudyMinder.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class PeriodoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime data)
            {
                // O parâmetro do conversor indica se a visualização é semanal
                bool isSemanal = parameter is string s && s.Equals("Semanal", StringComparison.OrdinalIgnoreCase);

                if (isSemanal)
                {
                    var inicioSemana = DateUtils.GetInicioSemana(data);
                    var fimSemana = inicioSemana.AddDays(6);
                    return $"{inicioSemana:dd/MM} - {fimSemana:dd/MM/yyyy}";
                }
                else
                {
                    // Formato "Mês YYYY" com a primeira letra maiúscula
                    return culture.TextInfo.ToTitleCase(data.ToString("MMMM yyyy", culture));
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
