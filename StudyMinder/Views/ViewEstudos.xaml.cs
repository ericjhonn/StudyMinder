using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class ViewEstudos : UserControl
    {
        public ViewEstudos()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewEstudos] Loaded - DataContext: {DataContext?.GetType().Name ?? "null"}");
            
            if (DataContext is EstudosViewModel viewModel)
            {
                viewModel.EstudosAtualizados += ViewModel_EstudosAtualizados;
                
                // Forçar atualização de bindings quando a view é carregada
                // Isso resolve o problema de LoadingAndEmptyStatePanel não atualizar ao navegar
                this.DataContext = null;
                this.DataContext = viewModel;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewEstudos] Unloaded");
            
            if (DataContext is EstudosViewModel viewModel)
            {
                viewModel.EstudosAtualizados -= ViewModel_EstudosAtualizados;
            }
        }

        private void ViewModel_EstudosAtualizados(object? sender, EventArgs e)
        {
            // Forçar uma atualização suave da visualização
            if (EstudosListView != null && EstudosListView.ItemsSource is ICollectionView collectionView)
            {
                collectionView.Refresh();
            }
            
            // Opcional: rolar para o topo após atualização
            if (EstudosListView != null && EstudosListView.Items.Count > 0)
            {
                EstudosListView.ScrollIntoView(EstudosListView.Items[0]);
            }
        }
    }
}
