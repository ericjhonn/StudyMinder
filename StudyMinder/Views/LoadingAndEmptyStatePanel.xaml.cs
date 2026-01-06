using System.Windows;
using System.Windows.Controls;

namespace StudyMinder.Views
{
    /// <summary>
    /// Painel reutilizável que exibe Loading Indicator e Estado Vazio
    /// 
    /// Uso:
    /// <views:LoadingAndEmptyStatePanel Grid.Row="0" />
    /// 
    /// Requer no ViewModel:
    /// - IsCarregando (bool) - Exibe loading quando true
    /// - FilteredCount (int) - Exibe estado vazio quando == 0 e IsCarregando == false
    /// </summary>
    public partial class LoadingAndEmptyStatePanel : UserControl
    {
        public LoadingAndEmptyStatePanel()
        {
            InitializeComponent();
            
            // Garantir que o painel sempre use o DataContext da View pai
            // Isso resolve o problema de DataContext perdido ao navegar entre views
            this.DataContextChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[LoadingAndEmptyStatePanel] DataContext alterado para: {e.NewValue?.GetType().Name ?? "null"}");
            };
        }
    }
}
