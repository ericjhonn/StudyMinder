using StudyMinder.Models;
using StudyMinder.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        public void ExibirDetalhes(EventosDia eventos, ICommand moverEventoCommand, ICommand editarEstudoCommand, ICommand iniciarRevisaoCommand)
        {
            if (eventos == null)
                return;

            // Atualizar dados com os comandos do ViewModel
            var viewModel = new DiaDetalhesViewModel(eventos, moverEventoCommand, editarEstudoCommand, iniciarRevisaoCommand);
            this.DataContext = viewModel;

            // Mostrar/ocultar seções baseado em conteúdo
            PanelEstudos.Visibility = eventos.Estudos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelEventos.Visibility = eventos.EventosEditais.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelRevisoes.Visibility = eventos.Revisoes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Mostrar o painel
            this.Visibility = Visibility.Visible;
        }
    }

    public class DiaDetalhesViewModel : ObservableObject
    {
        private readonly EventosDia _eventos;
        private readonly ICommand _moverEventoCommand;
        private readonly ICommand _editarEstudoCommand;
        private readonly ICommand _iniciarRevisaoCommand;

        public DiaDetalhesViewModel(
            EventosDia eventos, 
            ICommand moverEventoCommand,
            ICommand editarEstudoCommand,
            ICommand iniciarRevisaoCommand)
        {
            _eventos = eventos;
            _moverEventoCommand = moverEventoCommand;
            _editarEstudoCommand = editarEstudoCommand;
            _iniciarRevisaoCommand = iniciarRevisaoCommand;
        }

        public string DataFormatada => _eventos.Data.ToString("dddd, dd 'de' MMMM 'de' yyyy");
        public string Resumo => _eventos.ResumoEventos;
        public List<Estudo> Estudos => _eventos.Estudos;
        public List<EditalCronograma> EventosEditais => _eventos.EventosEditais;
        public List<Revisao> Revisoes => _eventos.Revisoes;
        
        public ICommand MoverEventoCommand => _moverEventoCommand;
        public ICommand EditarEstudoCommand => _editarEstudoCommand;
        public ICommand IniciarRevisaoCommand => _iniciarRevisaoCommand;
    }
}
