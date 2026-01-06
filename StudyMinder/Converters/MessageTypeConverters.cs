using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StudyMinder.Services;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Converte MessageType para cor de fundo (Success=Verde, Error=Vermelho, Warning=Laranja, Info=Azul)
    /// </summary>
    public class MessageTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                return messageType switch
                {
                    MessageType.Success => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),      // Verde
                    MessageType.Error => new SolidColorBrush(Color.FromArgb(255, 244, 67, 54)),        // Vermelho
                    MessageType.Warning => new SolidColorBrush(Color.FromArgb(255, 255, 152, 0)),      // Laranja
                    MessageType.Info => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)),        // Azul
                    _ => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converte MessageType para ícone (✓, ✕, ⚠, ℹ)
    /// </summary>
    public class MessageTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                return messageType switch
                {
                    MessageType.Success => "✓",
                    MessageType.Error => "✕",
                    MessageType.Warning => "⚠",
                    MessageType.Info => "ℹ",
                    _ => "ℹ"
                };
            }
            return "ℹ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converte MessageType para cor de borda/destaque
    /// </summary>
    public class MessageTypeToAccentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                return messageType switch
                {
                    MessageType.Success => new SolidColorBrush(Color.FromArgb(255, 56, 142, 60)),      // Verde escuro
                    MessageType.Error => new SolidColorBrush(Color.FromArgb(255, 211, 47, 47)),        // Vermelho escuro
                    MessageType.Warning => new SolidColorBrush(Color.FromArgb(255, 230, 124, 0)),      // Laranja escuro
                    MessageType.Info => new SolidColorBrush(Color.FromArgb(255, 13, 110, 253)),        // Azul escuro
                    _ => new SolidColorBrush(Color.FromArgb(255, 13, 110, 253))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
