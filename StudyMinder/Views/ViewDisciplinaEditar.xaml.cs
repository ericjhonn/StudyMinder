using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class ViewDisciplinaEditar : UserControl
    {
        private bool _isColorPaletteVisible = false;

        public ViewDisciplinaEditar()
        {
            InitializeComponent();
        }

        private void ToggleColorsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var buttonContent = button?.Content as StackPanel;
            var toggleIcon = buttonContent?.Children[0] as PackIconMaterial;

            if (!_isColorPaletteVisible)
            {
                // Mostrar paleta
                ColorPalette.Visibility = Visibility.Visible;
                if (toggleIcon != null) toggleIcon.Kind = PackIconMaterialKind.ChevronUp;
                _isColorPaletteVisible = true;
            }
            else
            {
                // Ocultar paleta
                ColorPalette.Visibility = Visibility.Collapsed;
                if (toggleIcon != null) toggleIcon.Kind = PackIconMaterialKind.ChevronDown;
                _isColorPaletteVisible = false;
            }
        }
    }
}
