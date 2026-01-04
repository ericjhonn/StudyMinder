using System.Globalization;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class HorasFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double horas = 0;

            if (value is double d)
            {
                horas = d;
            }
            else if (value is int i)
            {
                horas = (double)i;
            }
            else if (value is float f)
            {
                horas = (double)f;
            }
            else
            {
                return "0h00m";
            }

            int h = (int)horas;
            int m = (int)((horas - h) * 60);
            return $"{h}h{m:D2}m";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
