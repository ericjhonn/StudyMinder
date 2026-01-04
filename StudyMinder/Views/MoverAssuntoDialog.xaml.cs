using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StudyMinder.Models;
using StudyMinder.Services;
using static StudyMinder.Services.NotificationService;

namespace StudyMinder.Views
{
    public partial class MoverAssuntoDialog : Window, INotifyPropertyChanged
    {
        private readonly DisciplinaService _disciplinaService;
        private readonly AssuntoService _assuntoService;
        private Assunto _assuntoSelecionado;
        private Disciplina _disciplinaOrigem;
        private Disciplina? _disciplinaDestino;
        private ObservableCollection<Disciplina> _disciplinasDisponiveis = new();
        private int _totalEstudos = 0;
        private int _totalQuestoes = 0;
        private bool _podeMover = false;

        public MoverAssuntoDialog(DisciplinaService disciplinaService, AssuntoService assuntoService, 
            Assunto assunto, Disciplina disciplinaOrigem)
        {
            InitializeComponent();
            DataContext = this;

            _disciplinaService = disciplinaService ?? throw new ArgumentNullException(nameof(disciplinaService));
            _assuntoService = assuntoService ?? throw new ArgumentNullException(nameof(assuntoService));
            _assuntoSelecionado = assunto ?? throw new ArgumentNullException(nameof(assunto));
            _disciplinaOrigem = disciplinaOrigem ?? throw new ArgumentNullException(nameof(disciplinaOrigem));

            _ = Task.Run(CarregarDadosAsync);
        }

        public Assunto AssuntoSelecionado
        {
            get => _assuntoSelecionado;
            set => SetProperty(ref _assuntoSelecionado, value);
        }

        public Disciplina DisciplinaOrigem
        {
            get => _disciplinaOrigem;
            set => SetProperty(ref _disciplinaOrigem, value);
        }

        public Disciplina? DisciplinaDestino
        {
            get => _disciplinaDestino;
            set
            {
                if (SetProperty(ref _disciplinaDestino, value))
                {
                    PodeMover = value != null && value.Id != DisciplinaOrigem.Id;
                }
            }
        }

        public ObservableCollection<Disciplina> DisciplinasDisponiveis
        {
            get => _disciplinasDisponiveis;
            set => SetProperty(ref _disciplinasDisponiveis, value);
        }

        public int TotalEstudos
        {
            get => _totalEstudos;
            set => SetProperty(ref _totalEstudos, value);
        }

        public int TotalQuestoes
        {
            get => _totalQuestoes;
            set => SetProperty(ref _totalQuestoes, value);
        }

        public bool PodeMover
        {
            get => _podeMover;
            set => SetProperty(ref _podeMover, value);
        }

        private async Task CarregarDadosAsync()
        {
            try
            {
                // Carregar disciplinas disponíveis
                var disciplinas = await _disciplinaService.ObterTodasAsync();
                
                // Remover a disciplina atual da lista
                var disciplinasDisponiveis = disciplinas.Where(d => d.Id != DisciplinaOrigem.Id).ToList();

                // Carregar estatísticas do assunto (simulado por enquanto)
                // TODO: Implementar métodos no AssuntoService para obter essas estatísticas
                var totalEstudos = 0; // await _assuntoService.ObterTotalEstudosAsync(AssuntoSelecionado.Id);
                var totalQuestoes = 0; // await _assuntoService.ObterTotalQuestoesAsync(AssuntoSelecionado.Id);

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    DisciplinasDisponiveis.Clear();
                    foreach (var disciplina in disciplinasDisponiveis)
                    {
                        DisciplinasDisponiveis.Add(disciplina);
                    }

                    TotalEstudos = totalEstudos;
                    TotalQuestoes = totalQuestoes;
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    NotificationService.Instance.ShowError("Erro ao Carregar", 
                        $"Erro ao carregar dados: {ex.Message}");
                });
            }
        }

        private async void MoverAssunto_Click(object sender, RoutedEventArgs e)
        {
            if (DisciplinaDestino == null)
            {
                NotificationService.Instance.ShowWarning("Aviso", 
                    "Por favor, selecione uma disciplina de destino.");
                return;
            }

            var resultado = NotificationService.Instance.ShowConfirmation(
                "Confirmar Movimentação",
                $"Deseja realmente mover o assunto '{AssuntoSelecionado.Nome}' " +
                $"da disciplina '{DisciplinaOrigem.Nome}' para '{DisciplinaDestino.Nome}'?\n\n" +
                "Esta operação moverá todos os dados relacionados ao assunto e não poderá ser desfeita.");

            if (resultado != ToastMessageBoxResult.Yes)
                return;

            try
            {
                // Desabilitar botão durante a operação
                PodeMover = false;

                // Realizar a movimentação
                await _assuntoService.MoverParaDisciplinaAsync(AssuntoSelecionado.Id, DisciplinaDestino.Id);

                // Notificar sucesso com Toast
                NotificationService.Instance.ShowSuccess(
                    "Movimentação Concluída",
                    $"Assunto '{AssuntoSelecionado.Nome}' movido com sucesso para '{DisciplinaDestino.Nome}'!");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                PodeMover = true;
                NotificationService.Instance.ShowError("Erro ao Mover", 
                    $"Erro ao mover assunto: {ex.Message}");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
