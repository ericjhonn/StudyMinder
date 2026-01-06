using System.Windows;
using StudyMinder.Services;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    /// <summary>
    /// Janela Modal Customizada para MessageBox.
    /// Code-behind reduzido apenas para tratar cliques, definir Result/DialogResult e fechar.
    /// </summary>
    public partial class CustomMessageBoxWindow : Window
    {
        public CustomMessageBoxWindow()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                viewModel.Result = ToastMessageBoxResult.Ok;
            }
            DialogResult = true;
            Close();
        }

        private void BtnSim_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                viewModel.Result = ToastMessageBoxResult.Yes;
            }
            DialogResult = true;
            Close();
        }

        private void BtnNao_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                viewModel.Result = ToastMessageBoxResult.No;
            }
            DialogResult = false;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                viewModel.Result = ToastMessageBoxResult.Cancel;
            }
            DialogResult = false;
            Close();
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageBoxViewModel viewModel)
            {
                viewModel.Result = ToastMessageBoxResult.Cancel;
            }
            DialogResult = false;
            Close();
        }
    }
}
