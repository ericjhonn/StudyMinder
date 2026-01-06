using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StudyMinder.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Verifica se o parâmetro "Invert" foi passado no XAML
                bool invert = parameter is string paramStr &&
                              paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase);

                if (invert)
                {
                    // Modo Invertido: Usado para mensagens de "Lista Vazia"
                    // Se count > 0 (tem itens) -> Esconde (Collapsed)
                    // Se count == 0 (vazio) -> Mostra (Visible)
                    return count > 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    // Modo Padrão: Usado para a Lista em si
                    // Se count > 0 (tem itens) -> Mostra (Visible)
                    // Se count == 0 (vazio) -> Esconde (Collapsed)
                    return count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}