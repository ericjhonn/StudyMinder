using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StudyMinder.Views
{
    public partial class ViewEditalEditarCronograma : UserControl
    {
        private ListView? _eventosListView;

        public ViewEditalEditarCronograma()
        {
            InitializeComponent();
            this.Loaded += ViewEditalEditarCronograma_Loaded;
        }

        private void ViewEditalEditarCronograma_Loaded(object sender, RoutedEventArgs e)
        {
            // Encontrar o ListView
            _eventosListView = this.FindName("EventosListView") as ListView;
            if (_eventosListView != null)
            {
                // Permitir que o scroll funcione quando o mouse está sobre os itens
                _eventosListView.PreviewMouseWheel += EventosListView_PreviewMouseWheel;
            }
        }

        private void EventosListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Não marcar como handled - deixar o ListView processar normalmente
            // Isso permite que o scroll funcione naturalmente
            e.Handled = false;
        }
    }
}
