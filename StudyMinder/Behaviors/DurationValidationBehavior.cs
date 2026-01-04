using Microsoft.Xaml.Behaviors;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace StudyMinder.Behaviors
{
    /// <summary>
    /// Behavior para validar entrada de duração no formato HH:MM:SS
    /// </summary>
    public class DurationValidationBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += TextBox_PreviewTextInput;
            AssociatedObject.TextChanged += TextBox_TextChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= TextBox_PreviewTextInput;
            AssociatedObject.TextChanged -= TextBox_TextChanged;
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Permitir apenas dígitos e dois-pontos
            if (!Regex.IsMatch(e.Text, @"[0-9:]"))
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text;

            // Remover caracteres inválidos
            text = Regex.Replace(text, @"[^0-9:]", "");

            // Limitar a 8 caracteres (HH:MM:SS)
            if (text.Length > 8)
            {
                text = text.Substring(0, 8);
            }

            // Formatar automaticamente
            text = AutoFormatDuration(text);

            if (textBox.Text != text)
            {
                textBox.Text = text;
                textBox.CaretIndex = text.Length;
            }
        }

        private string AutoFormatDuration(string input)
        {
            // Remover dois-pontos existentes
            string digits = Regex.Replace(input, ":", "");

            if (digits.Length == 0) return "";
            if (digits.Length <= 2) return digits;
            if (digits.Length <= 4) return digits.Substring(0, 2) + ":" + digits.Substring(2);
            if (digits.Length <= 6) return digits.Substring(0, 2) + ":" + digits.Substring(2, 2) + ":" + digits.Substring(4);

            // Máximo 6 dígitos (HH:MM:SS)
            return digits.Substring(0, 2) + ":" + digits.Substring(2, 2) + ":" + digits.Substring(4, 2);
        }
    }
}
