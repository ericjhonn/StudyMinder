using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Conversor que gerencia a visibilidade de Loading Indicator e Estado Vazio
    /// Garante que apenas um estado seja visível por vez
    /// 
    /// Uso:
    /// - Para Loading: ConverterParameter="loading"
    /// - Para Empty: ConverterParameter="empty"
    /// 
    /// Bindings:
    /// 1. IsCarregando (bool)
    /// 2. FilteredCount (int)
    /// </summary>
    public class LoadingAndEmptyStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            bool isCarregando = values[0] is bool b && b;
            int filteredCount = values[1] is int i ? i : 0;
            string state = parameter?.ToString() ?? string.Empty;

            // Prioridade: Loading > Empty > Hidden
            if (isCarregando)
            {
                // Enquanto carregando, mostrar apenas Loading
                return state == "loading" ? Visibility.Visible : Visibility.Collapsed;
            }

            // Após carregamento, se não há dados, mostrar Empty
            if (filteredCount == 0)
            {
                return state == "empty" ? Visibility.Visible : Visibility.Collapsed;
            }

            // Se há dados, ocultar ambos (ListView será visível)
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
