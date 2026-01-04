using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class ViewComparadorEditais : UserControl
    {
        public ViewComparadorEditais()
        {
            InitializeComponent();

            // Resolução de Dependência via App.ServiceProvider
            if (Application.Current is App app)
            {
                DataContext = app.ServiceProvider.GetRequiredService<ComparadorEditaisViewModel>();
            }
        }
    }
}