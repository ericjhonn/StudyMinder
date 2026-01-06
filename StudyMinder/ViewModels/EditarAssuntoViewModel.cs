using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StudyMinder.ViewModels
{
    /// <summary>
    /// ViewModel para edição/criação de assuntos.
    /// NÃO persiste diretamente no banco - retorna o assunto modificado para o EditarDisciplinaViewModel.
    /// </summary>
    public partial class EditarAssuntoViewModel : ObservableValidator, IDisposable
    {
        private readonly AssuntoService? _assuntoService;
        private readonly NavigationService? _navigationService;
        private readonly INotificationService _notificationService;
        private Assunto _assunto;
        private readonly int _disciplinaId;
        private CancellationTokenSource? _debounceTokenSource;
        private bool _disposed;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isSaving;

        // Construtor sem parâmetros para o designer XAML
        public EditarAssuntoViewModel() : this(null!, null!, NotificationService.Instance, new Assunto(), 0)
        {
        }

        public EditarAssuntoViewModel(
            AssuntoService assuntoService,
            NavigationService navigationService,
            INotificationService notificationService,
            Assunto? assunto,
            int disciplinaId)
        {
            _assuntoService = assuntoService;
            _navigationService = navigationService;
            _notificationService = notificationService;
            _disciplinaId = disciplinaId;

            if (assunto == null || assunto.Id == 0)
            {
                _assunto = new Assunto { DisciplinaId = disciplinaId };
                Title = "Novo Assunto";
                IsEditing = false;
            }
            else
            {
                _assunto = assunto;
                Title = "Editar Assunto";
                IsEditing = true;
            }

            // Inicializar propriedades
            Nome = _assunto.Nome;
            Concluido = _assunto.Concluido;
            CadernoQuestoes = _assunto.CadernoQuestoes ?? string.Empty;
        }

        private string _nome = string.Empty;
        [Required(ErrorMessage = "O nome do assunto é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome
        {
            get => _nome;
            set
            {
                if (SetProperty(ref _nome, value, true))
                {
                    ValidateNomeAsync(value);
                }
            }
        }

        private bool _concluido;
        public bool Concluido
        {
            get => _concluido;
            set => SetProperty(ref _concluido, value);
        }

        private string _linkCadernoQuestoes = string.Empty;
        public string CadernoQuestoes
        {
            get => _linkCadernoQuestoes;
            set => SetProperty(ref _linkCadernoQuestoes, value ?? string.Empty);
        }

        // Propriedades calculadas removidas - agora são calculadas diretamente no model

        /// <summary>
        /// Salva as alterações do assunto e retorna para o EditarDisciplinaViewModel.
        /// NÃO persiste no banco de dados.
        /// </summary>
        [RelayCommand]
        private async Task SalvarAsync()
        {
            if (_navigationService == null) return;

            IsSaving = true;

            try
            {
                ValidateAllProperties();

                if (HasErrors)
                {
                    var errors = GetAllErrors();
                    _notificationService.ShowWarning(
                        "Erros de Validação",
                        $"Por favor, corrija os seguintes erros:\n\n{string.Join("\n", errors)}");
                    return;
                }

                // Validação assíncrona de nome duplicado
                if (_assuntoService != null)
                {
                    bool nomeExiste = await _assuntoService.NomeExisteAsync(
                        Nome,
                        _disciplinaId,
                        _assunto.Id > 0 ? _assunto.Id : null);

                    if (nomeExiste)
                    {
                        _notificationService.ShowWarning(
                            "Nome Duplicado",
                            "Já existe um assunto com este nome nesta disciplina.");
                        return;
                    }
                }

                // Atualizar o assunto em memória
                _assunto.Nome = Nome;
                _assunto.Concluido = Concluido;
                _assunto.CadernoQuestoes = CadernoQuestoes;
                _assunto.DisciplinaId = _disciplinaId;

                // Retornar o assunto para o EditarDisciplinaViewModel via NavigationService
                _navigationService.SetNavigationResult(_assunto);
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError(
                    "Erro ao Validar",
                    $"Erro ao validar o assunto: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancelar()
        {
            _navigationService?.GoBack();
        }

        private void ValidateNomeAsync(string nome)
        {
            if (_assuntoService == null) return;

            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            Task.Delay(500, token).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;

                ClearErrors(nameof(Nome));
                if (string.IsNullOrWhiteSpace(nome)) return;

                bool exists = await _assuntoService.NomeExisteAsync(
                    nome,
                    _disciplinaId,
                    _assunto.Id > 0 ? _assunto.Id : null);

                if (exists)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        AddError(nameof(Nome), "Este nome de assunto já está em uso nesta disciplina.");
                    });
                }
            }, token);
        }

        private void AddError(string propertyName, string errorMessage)
        {
            if (!_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors[propertyName] = new List<string>();
            }

            if (!_validationErrors[propertyName].Contains(errorMessage))
            {
                _validationErrors[propertyName].Add(errorMessage);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private readonly Dictionary<string, List<string>> _validationErrors = new Dictionary<string, List<string>>();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName != null && _validationErrors.ContainsKey(e.PropertyName))
            {
                _validationErrors.Remove(e.PropertyName);
            }
        }

        public new IEnumerable GetErrors(string? propertyName)
        {
            var allErrors = new List<string>();

            var baseErrors = base.GetErrors(propertyName);
            if (baseErrors != null)
            {
                foreach (var error in baseErrors)
                {
                    var validationResult = error as ValidationResult;
                    if (validationResult != null && validationResult.ErrorMessage != null)
                    {
                        allErrors.Add(validationResult.ErrorMessage);
                    }
                }
            }

            if (propertyName != null && _validationErrors.ContainsKey(propertyName))
            {
                allErrors.AddRange(_validationErrors[propertyName]);
            }

            return allErrors;
        }

        private List<string> GetAllErrors()
        {
            var errors = new List<string>();

            var properties = new[] { nameof(Nome), nameof(CadernoQuestoes) };
            foreach (var property in properties)
            {
                var propertyErrors = GetErrors(property)?.Cast<string>();
                if (propertyErrors != null && propertyErrors.Any())
                {
                    errors.AddRange(propertyErrors);
                }
            }

            return errors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _debounceTokenSource?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
