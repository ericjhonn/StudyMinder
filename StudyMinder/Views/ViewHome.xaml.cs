using System.Windows.Controls;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class ViewHome : UserControl
    {
        public static readonly DateTime CurrentDate = DateTime.Now;

        public ViewHome()
        {
            InitializeComponent();
            // Remover Loaded event - deixar que o ViewModel carregue dados de forma assíncrona
        }
    }
}
