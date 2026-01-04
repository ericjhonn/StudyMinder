using System.Windows;
using System.Windows.Controls;
using StudyMinder.ViewModels;

namespace StudyMinder.Behaviors
{
    /// <summary>
    /// Behavior para interceptar descarregamento de views com edição
    /// NOTA: A verificação de alterações não salvas é feita em NavigationService.GoBack()
    /// Este behavior é mantido apenas para compatibilidade com views existentes
    /// </summary>
    public static class EditableViewBehavior
    {
        public static bool GetEnableEditableViewCheck(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableEditableViewCheckProperty);
        }

        public static void SetEnableEditableViewCheck(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableEditableViewCheckProperty, value);
        }

        public static readonly DependencyProperty EnableEditableViewCheckProperty =
            DependencyProperty.RegisterAttached(
                "EnableEditableViewCheck",
                typeof(bool),
                typeof(EditableViewBehavior),
                new PropertyMetadata(false, OnEnableEditableViewCheckChanged));

        private static void OnEnableEditableViewCheckChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Behavior desativado - verificação é feita em NavigationService.GoBack()
            // Mantém compatibilidade com XAML existente
        }
    }
}
