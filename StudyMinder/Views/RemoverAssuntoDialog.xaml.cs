using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using StudyMinder.Models;
using StudyMinder.Services;
using static StudyMinder.Services.NotificationService;

namespace StudyMinder.Views
{
    public partial class RemoverAssuntoDialog : Window, INotifyPropertyChanged
    {
        private readonly DisciplinaService _disciplinaService;
        private readonly AssuntoService _assuntoService;
        private Assunto _assuntoSelecionado;
        private Disciplina _disciplinaOrigem;
        private Disciplina? _disciplinaDestino;
        private Assunto? _assuntoDestino;
        private ObservableCollection<Disciplina> _disciplinasDisponiveis = new();
        private ObservableCollection<Assunto> _assuntosDestino = new();
        private int _totalEstudos = 0;
        private int _totalQuestoes = 0;
        private bool _removerEmCascata = false;
        private bool _podeRemover = false;

        /// <summary>
        /// Resultado da operação de remoção. Será definido quando o usuário confirma a remoção.
        /// Armazena as informações necessárias para remover/mover os estudos na disciplina.
        /// </summary>
        public Models.RemocaoAssuntoResultado? ResultadoRemocao { get; private set; }

        public RemoverAssuntoDialog(DisciplinaService disciplinaService, AssuntoService assuntoService, 
            Assunto assunto, Disciplina disciplinaOrigem)
        {
            InitializeComponent();
            DataContext = this;

            _disciplinaService = disciplinaService ?? throw new ArgumentNullException(nameof(disciplinaService));
            _assuntoService = assuntoService ?? throw new ArgumentNullException(nameof(assuntoService));
            _assuntoSelecionado = assunto ?? throw new ArgumentNullException(nameof(assunto));
            _disciplinaOrigem = disciplinaOrigem ?? throw new ArgumentNullException(nameof(disciplinaOrigem));

            _ = Task.Run(async () => await CarregarDadosAsync());
        }

        public Assunto AssuntoSelecionado
        {
            get => _assuntoSelecionado;
            set
            {
                if (SetProperty(ref _assuntoSelecionado, value))
                {
                    // Notificar mudanças nas propriedades calculadas
                    OnPropertyChanged(nameof(RemoverEstudosTexto));
                    OnPropertyChanged(nameof(ProgressoFormatado));
                    OnPropertyChanged(nameof(TaxaAcertoFormatada));
                    OnPropertyChanged(nameof(TotalAcertos));
                    OnPropertyChanged(nameof(TotalErros));
                    OnPropertyChanged(nameof(Rendimento));
                    OnPropertyChanged(nameof(HorasEstudadas));
                    OnPropertyChanged(nameof(Concluido));
                }
            }
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
                    CarregarAssuntosDestinoAsync();
                    AtualizarPodeRemover();
                }
            }
        }

        public Assunto? AssuntoDestino
        {
            get => _assuntoDestino;
            set
            {
                if (SetProperty(ref _assuntoDestino, value))
                {
                    AtualizarPodeRemover();
                }
            }
        }

        public ObservableCollection<Disciplina> DisciplinasDisponiveis
        {
            get => _disciplinasDisponiveis;
            set => SetProperty(ref _disciplinasDisponiveis, value);
        }

        public ObservableCollection<Assunto> AssuntosDestino
        {
            get => _assuntosDestino;
            set => SetProperty(ref _assuntosDestino, value);
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

        public bool RemoverEmCascata
        {
            get => _removerEmCascata;
            set
            {
                if (SetProperty(ref _removerEmCascata, value))
                {
                    AtualizarPodeRemover();
                    OnPropertyChanged(nameof(MoverEstudosVisibility));
                    OnPropertyChanged(nameof(AvisoVisibility));
                    OnPropertyChanged(nameof(MensagemAviso));
                }
            }
        }

        public bool PodeRemover
        {
            get => _podeRemover;
            set => SetProperty(ref _podeRemover, value);
        }

        public string RemoverEstudosTexto => 
            $"Remover todos os estudos vinculados ao assunto '{AssuntoSelecionado.Nome}'";

        public Visibility MoverEstudosVisibility => 
            RemoverEmCascata ? Visibility.Collapsed : Visibility.Visible;

        public Visibility AvisoVisibility => 
            RemoverEmCascata ? Visibility.Visible : Visibility.Collapsed;

        public string MensagemAviso => 
            RemoverEmCascata 
                ? $"Atenção: {TotalEstudos} estudo(s) será(ão) removido(s) permanentemente!" 
                : "";

        public string ProgressoFormatado
        {
            get
            {
                var progresso = AssuntoSelecionado?.Progresso ?? 0;
                return $"{progresso:P0}";
            }
        }

        public string TaxaAcertoFormatada
        {
            get
            {
                var taxa = AssuntoSelecionado?.Rendimento ?? 0;
                return $"{taxa:F0}%";
            }
        }

        public string TotalEstudosFormatado => 
            $"{TotalEstudos} estudo(s)";

        public string TotalQuestoesFormatado => 
            $"{TotalQuestoes} questão(ões)";

        public int TotalAcertos => 
            AssuntoSelecionado?.TotalAcertos ?? 0;

        public int TotalErros => 
            AssuntoSelecionado?.TotalErros ?? 0;

        public double Rendimento => 
            AssuntoSelecionado?.Rendimento ?? 0;

        public string HorasEstudadas => 
            AssuntoSelecionado?.HorasEstudadas ?? "0h00";

        public bool Concluido => 
            AssuntoSelecionado?.Concluido ?? false;

        private async Task CarregarDadosAsync()
        {
            try
            {
                // Carregar disciplinas disponíveis
                var disciplinas = await _disciplinaService.ObterTodasAsync();
                
                // Remover a disciplina atual da lista
                var disciplinasDisponiveis = disciplinas.Where(d => d.Id != DisciplinaOrigem.Id).ToList();

                // Carregar estatísticas do assunto
                var totalEstudos = AssuntoSelecionado?.Estudos?.Count ?? 0;
                var totalQuestoes = AssuntoSelecionado?.CadernoQuestoes?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    DisciplinasDisponiveis.Clear();
                    foreach (var disciplina in disciplinasDisponiveis)
                    {
                        DisciplinasDisponiveis.Add(disciplina);
                    }

                    TotalEstudos = totalEstudos;
                    TotalQuestoes = totalQuestoes;

                    // Notificar mudanças nas propriedades calculadas
                    OnPropertyChanged(nameof(RemoverEstudosTexto));
                    OnPropertyChanged(nameof(ProgressoFormatado));
                    OnPropertyChanged(nameof(TaxaAcertoFormatada));
                    OnPropertyChanged(nameof(TotalEstudosFormatado));
                    OnPropertyChanged(nameof(TotalQuestoesFormatado));
                    OnPropertyChanged(nameof(TotalAcertos));
                    OnPropertyChanged(nameof(TotalErros));
                    OnPropertyChanged(nameof(Rendimento));
                    OnPropertyChanged(nameof(HorasEstudadas));
                    OnPropertyChanged(nameof(Concluido));
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

        private async Task CarregarAssuntosDestinoAsync()
        {
            if (DisciplinaDestino == null)
            {
                AssuntosDestino.Clear();
                return;
            }

            try
            {
                var assuntos = await _assuntoService.ObterPorDisciplinaAsync(DisciplinaDestino.Id);

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    AssuntosDestino.Clear();
                    foreach (var assunto in assuntos.OrderBy(a => a.Nome))
                    {
                        AssuntosDestino.Add(assunto);
                    }

                    // Limpar seleção de assunto ao mudar disciplina
                    AssuntoDestino = null;
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    NotificationService.Instance.ShowError("Erro ao Carregar Assuntos", 
                        $"Erro ao carregar assuntos: {ex.Message}");
                });
            }
        }

        private void AtualizarPodeRemover()
        {
            if (RemoverEmCascata)
            {
                // Pode remover se checkbox está marcado
                PodeRemover = true;
            }
            else
            {
                // Pode remover se ambos os comboboxes estão preenchidos
                PodeRemover = DisciplinaDestino != null && AssuntoDestino != null;
            }
        }

        private async void RemoverAssunto_Click(object sender, RoutedEventArgs e)
        {
            if (RemoverEmCascata)
            {
                // Remover em cascata (sem mover estudos)
                //var resultado = NotificationService.Instance.ShowConfirmation(
                //    "Confirmar Remoção em Cascata",
                //    $"Deseja realmente remover o assunto '{AssuntoSelecionado.Nome}'?\n\n" +
                //    $"AVISO: {TotalEstudos} estudo(s) será(ão) removido(s) permanentemente!\n\n" +
                //    "Esta ação não poderá ser desfeita.");

                //if (resultado != ToastMessageBoxResult.Yes)
                //    return;

                try
                {
                    PodeRemover = false;
                    
                    // Criar resultado de remoção em cascata
                    ResultadoRemocao = new Models.RemocaoAssuntoResultado(
                        AssuntoSelecionado.Id,
                        removerEmCascata: true,
                        TotalEstudos);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    PodeRemover = true;
                    NotificationService.Instance.ShowError("Erro ao Remover", 
                        $"Erro ao processar remoção: {ex.Message}");
                }
            }
            else if (DisciplinaDestino != null && AssuntoDestino != null)
            {
                // Mover estudos para outro assunto
                //var resultado = NotificationService.Instance.ShowConfirmation(
                //    "Confirmar Remoção com Movimentação",
                //    $"Deseja remover o assunto '{AssuntoSelecionado.Nome}'?\n\n" +
                //    $"Os {TotalEstudos} estudo(s) vinculado(s) serão movido(s) para o assunto " +
                //    $"'{AssuntoDestino.Nome}' na disciplina '{DisciplinaDestino.Nome}'.");

                //if (resultado != ToastMessageBoxResult.Yes)
                //    return;

                try
                {
                    PodeRemover = false;
                    
                    // Criar resultado de remoção com movimentação
                    ResultadoRemocao = new Models.RemocaoAssuntoResultado(
                        AssuntoSelecionado.Id,
                        AssuntoDestino.Id,
                        DisciplinaDestino.Id,
                        TotalEstudos);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    PodeRemover = true;
                    NotificationService.Instance.ShowError("Erro ao Remover", 
                        $"Erro ao processar remoção: {ex.Message}");
                }
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
