using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StudyMinder.Models;

namespace StudyMinder.Converters
{
    public class TipoEstudoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PizzaChartData pizzaData)
            {
                return TipoEstudoColorMap.GetBrush(pizzaData.TipoEstudoNome);
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
