using System.Windows;

namespace StudyMinder.Views
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Atualiza a mensagem de status do SplashScreen
        /// </summary>
        public void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        /// <summary>
        /// Define a barra de progresso como determinada (não indeterminada)
        /// </summary>
        public void SetProgress(double value)
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = value;
        }

        /// <summary>
        /// Fecha o SplashScreen
        /// </summary>
        public void CloseSplash()
        {
            this.Close();
        }
    }
}
