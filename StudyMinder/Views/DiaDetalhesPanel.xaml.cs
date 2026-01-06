using System.Windows;
using System.Windows.Controls;
using StudyMinder.Models;
using StudyMinder.ViewModels;

namespace StudyMinder.Views
{
    public partial class DiaDetalhesPanel : UserControl
    {
        public DiaDetalhesPanel()
        {
            InitializeComponent();
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            // Fechar o painel
            this.Visibility = Visibility.Collapsed;
        }

        public void ExibirDetalhes(EventosDia eventos)
        {
            if (eventos == null)
                return;

            // Atualizar dados
            var viewModel = new DiaDetalhesViewModel(eventos);
            this.DataContext = viewModel;

            // Mostrar/ocultar seções baseado em conteúdo
            PanelEstudos.Visibility = eventos.Estudos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelEventos.Visibility = eventos.EventosEditais.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelRevisoes.Visibility = eventos.Revisoes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Mostrar o painel
            this.Visibility = Visibility.Visible;
        }
    }

    public class DiaDetalhesViewModel
    {
        private readonly EventosDia _eventos;

        public DiaDetalhesViewModel(EventosDia eventos)
        {
            _eventos = eventos;
        }

        public string DataFormatada => _eventos.Data.ToString("dddd, dd 'de' MMMM 'de' yyyy");
        public string Resumo => _eventos.ResumoEventos;
        public List<Estudo> Estudos => _eventos.Estudos;
        public List<EditalCronograma> EventosEditais => _eventos.EventosEditais;
        public List<Revisao> Revisoes => _eventos.Revisoes;
    }
}
