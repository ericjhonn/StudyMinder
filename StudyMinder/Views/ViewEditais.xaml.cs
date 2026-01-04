using System.Windows.Controls;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class ViewEditais : UserControl
    {
        public ViewEditais()
        {
            InitializeComponent();
            Loaded += ViewEditais_Loaded;
        }

        private void ViewEditais_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewEditais] Loaded - DataContext: {DataContext?.GetType().Name ?? "null"}");
            
            // Recarregar dados quando a view for exibida
            if (DataContext is EditaisViewModel viewModel)
            {
                // Forçar atualização de bindings quando a view é carregada
                // Isso resolve o problema de LoadingAndEmptyStatePanel não atualizar ao navegar
                this.DataContext = null;
                this.DataContext = viewModel;
            }
        }
    }
}
