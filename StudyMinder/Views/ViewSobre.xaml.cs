using System.Windows.Controls;

namespace StudyMinder.Views
{
    /// <summary>
    /// Interaction logic for ViewSobre.xaml
    /// </summary>
    public partial class ViewSobre : UserControl
    {
        public ViewSobre()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}