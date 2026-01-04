using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using StudyMinder.ViewModels;
using Microsoft.EntityFrameworkCore;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Views;

namespace StudyMinder.Navigation
{
    public static class NavigationServiceExtensions
    {
        public static void NavigateTo<TViewModel>(this NavigationService navigationService, object? parameter = null)
            where TViewModel : class
        {
            var view = CreateViewForViewModel<TViewModel>(navigationService, parameter);
            navigationService.NavigateTo(view);
        }

        private static UserControl CreateViewForViewModel<TViewModel>(NavigationService navigationService, object? parameter = null)
            where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);

            // Mapeamento de ViewModels para Views
            // Padrão: EditarEditalViewModel -> ViewEditalEditar
            // Remove "ViewModel" do final
            var baseNameWithoutViewModel = viewModelType.Name.Replace("ViewModel", "");

            // Se começa com "Editar", transforma em "Edital" + "Editar"
            // Exemplo: EditarEditalViewModel -> EditalEditar
            string viewTypeName;
            if (baseNameWithoutViewModel.StartsWith("Editar"))
            {
                var moduleName = baseNameWithoutViewModel.Substring(6); // Remove "Editar"
                viewTypeName = "View" + moduleName + "Editar";
            }
            else
            {
                viewTypeName = "View" + baseNameWithoutViewModel;
            }

            var viewType = Type.GetType($"StudyMinder.Views.{viewTypeName}")
                          ?? throw new TypeLoadException($"View não encontrada para ViewModel {viewModelType.Name}. Procurando: StudyMinder.Views.{viewTypeName}");

            var view = (UserControl)Activator.CreateInstance(viewType)!;

            // Criar ViewModel com injeção de dependência (simplificado)
            var viewModel = CreateViewModel<TViewModel>(navigationService, parameter);
            view.DataContext = viewModel;

            return view;
        }

        private static TViewModel CreateViewModel<TViewModel>(NavigationService navigationService, object? parameter = null)
            where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);

            // Para EditarEditalViewModel, precisamos passar os serviços
            if (viewModelType == typeof(EditarEditalViewModel))
            {
                // Usar contexto compartilhado ou criar um novo
                var context = navigationService._sharedContext;
                if (context == null)
                {
                    var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var dbPath = System.IO.Path.Combine(exeDir, "StudyMinder.db");
                    if (!System.IO.File.Exists(dbPath))
                    {
                        dbPath = System.IO.Path.Combine(System.IO.Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
                    }

                    var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                    context = new Data.StudyMinderContext(optionsBuilder.Options);
                }

                var editalService = new Services.EditalService(context, new Services.AuditoriaService());
                var revisaoNotificacaoService = navigationService._revisaoNotificacaoService ?? new Services.RevisaoNotificacaoService();

                if (parameter is Models.Edital edital)
                {
                    return (TViewModel)(object)new EditarEditalViewModel(editalService, navigationService, context, revisaoNotificacaoService, NotificationService.Instance, edital);
                }
                else
                {
                    return (TViewModel)(object)new EditarEditalViewModel(editalService, navigationService, context, revisaoNotificacaoService, NotificationService.Instance);
                }
            }

            // Para DisciplinasViewModel
            if (viewModelType == typeof(DisciplinasViewModel))
            {
                var context = navigationService._sharedContext;
                if (context == null)
                {
                    var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var dbPath = System.IO.Path.Combine(exeDir, "StudyMinder.db");
                    if (!System.IO.File.Exists(dbPath))
                    {
                        dbPath = System.IO.Path.Combine(System.IO.Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
                    }

                    var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                    context = new Data.StudyMinderContext(optionsBuilder.Options);
                }

                var auditoriaService = new Services.AuditoriaService();
                var disciplinaService = new Services.DisciplinaService(context, auditoriaService);
                var assuntoService = new Services.AssuntoService(context, auditoriaService, disciplinaService);
                var transactionService = new Services.DisciplinaAssuntoTransactionService(context, auditoriaService);

                return (TViewModel)(object)new DisciplinasViewModel(
                        disciplinaService,
                        assuntoService,
                        transactionService,
                        navigationService,
                        NotificationService.Instance,
                        App.ConfigurationService);
            }

            // Para EditarEstudoViewModel
            if (viewModelType == typeof(EditarEstudoViewModel))
            {
                var context = navigationService._sharedContext;
                if (context == null)
                {
                    var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var dbPath = System.IO.Path.Combine(exeDir, "StudyMinder.db");
                    if (!System.IO.File.Exists(dbPath))
                    {
                        dbPath = System.IO.Path.Combine(System.IO.Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
                    }

                    var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                    context = new Data.StudyMinderContext(optionsBuilder.Options);
                }

                var auditoriaService = new Services.AuditoriaService();
                var estudoService = new Services.EstudoService(context, auditoriaService);
                var tipoEstudoService = new Services.TipoEstudoService(context, auditoriaService);
                var disciplinaService = new Services.DisciplinaService(context, auditoriaService);
                var assuntoService = new Services.AssuntoService(context, auditoriaService, disciplinaService);
                var transactionService = new Services.EstudoTransactionService(context, auditoriaService);
                var revisaoService = new Services.RevisaoService(context, auditoriaService);
                var configurationService = App.ConfigurationService;

                Estudo? estudoParam = null;
                if (parameter is Estudo estudo)
                {
                    estudoParam = estudo;
                }

                return (TViewModel)(object)new EditarEstudoViewModel(
                    estudoService,
                    tipoEstudoService,
                    assuntoService,
                    disciplinaService,
                    transactionService,
                    navigationService,
                    revisaoService,
                    NotificationService.Instance,
                    configurationService,
                    estudoParam);
            }

            // Para EditaisViewModel
            if (viewModelType == typeof(EditaisViewModel))
            {
                var context = navigationService._sharedContext;
                if (context == null)
                {
                    var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var dbPath = System.IO.Path.Combine(exeDir, "StudyMinder.db");
                    if (!System.IO.File.Exists(dbPath))
                    {
                        dbPath = System.IO.Path.Combine(System.IO.Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
                    }

                    var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                    context = new Data.StudyMinderContext(optionsBuilder.Options);
                }

                var editalService = new Services.EditalService(context, new Services.AuditoriaService());
                var transactionService = new Services.EditalTransactionService(context, new Services.AuditoriaService());
                var revisaoNotificacaoService = navigationService._revisaoNotificacaoService ?? new Services.RevisaoNotificacaoService();

                return (TViewModel)(object)new EditaisViewModel(
                    editalService,
                    transactionService,
                    navigationService,
                    revisaoNotificacaoService,
                    NotificationService.Instance,
                    App.ConfigurationService);
            }

            // Para EstudosViewModel
            // Para EstudosViewModel
            if (viewModelType == typeof(EstudosViewModel))
            {
                try
                {
                    var context = navigationService._sharedContext;
                    if (context == null)
                    {
                        var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                        var dbPath = System.IO.Path.Combine(exeDir, "StudyMinder.db");
                        if (!System.IO.File.Exists(dbPath))
                        {
                            dbPath = System.IO.Path.Combine(System.IO.Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
                        }

                        var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
                        optionsBuilder.UseSqlite($"Data Source={dbPath}");
                        context = new Data.StudyMinderContext(optionsBuilder.Options);
                    }

                    var auditoriaService = new Services.AuditoriaService();
                    var estudoService = new Services.EstudoService(context, auditoriaService);
                    var tipoEstudoService = new Services.TipoEstudoService(context, auditoriaService);
                    var disciplinaService = new Services.DisciplinaService(context, auditoriaService);
                    var assuntoService = new Services.AssuntoService(context, auditoriaService, disciplinaService);
                    var transactionService = new Services.EstudoTransactionService(context, auditoriaService);
                    var pomodoroTimerService = new Services.PomodoroTimerService();
                    var revisaoService = new Services.RevisaoService(context, auditoriaService);
                    var configurationService = App.ConfigurationService;

                    // CORREÇÃO 1: Instanciar o serviço de notificação de estudo que faltava
                    var estudoNotificacaoService = new Services.EstudoNotificacaoService();

                    return (TViewModel)(object)new EstudosViewModel(
                        estudoService,
                        tipoEstudoService,
                        assuntoService,
                        disciplinaService,
                        transactionService,
                        pomodoroTimerService,
                        navigationService,
                        estudoNotificacaoService,     // CORREÇÃO 2: Passar o EstudoNotificacaoService (Argumento 8)
                        revisaoService,
                        NotificationService.Instance, // CORREÇÃO 3: Passar o NotificationService (Argumento 10)
                        configurationService);        // CORREÇÃO 4: Passar o ConfigurationService (Argumento 11)
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[NavigationService] Erro ao criar EstudosViewModel: {ex.Message}");
                    throw;
                }
            }

            // Para outros ViewModels, usar construtor padrão
            return (TViewModel)Activator.CreateInstance(viewModelType)!;
        }
    }

    public class NavigationService
    {
        private readonly Stack<UserControl> _navigationStack = new();
        private readonly ContentControl _contentArea;
        private object? _navigationResult;
        internal readonly Data.StudyMinderContext? _sharedContext;
        internal readonly RevisaoNotificacaoService? _revisaoNotificacaoService;

        public event EventHandler<UserControl>? Navigated;

        public NavigationService(ContentControl contentArea, Data.StudyMinderContext? context = null, RevisaoNotificacaoService? revisaoNotificacaoService = null)
        {
            _contentArea = contentArea ?? throw new ArgumentNullException(nameof(contentArea));
            _sharedContext = context;
            _revisaoNotificacaoService = revisaoNotificacaoService;
        }

        public void NavigateTo(UserControl page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            if (_contentArea.Content != null)
            {
                _navigationStack.Push(_contentArea.Content as UserControl ?? throw new InvalidOperationException("Current content is not a UserControl"));
            }

            _contentArea.Content = page;
            Navigated?.Invoke(this, page);
        }

        public bool CanGoBack => _navigationStack.Count > 0;

        public async void GoBack()
        {
            if (!CanGoBack) return;

            // Verificar se o ViewModel atual implementa IEditableViewModel
            var currentContent = _contentArea.Content as UserControl;
            if (currentContent?.DataContext is IEditableViewModel editableViewModel)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔍 NavigationService.GoBack() - Verificando alterações não salvas em {currentContent.DataContext.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] HasUnsavedChanges: {editableViewModel.HasUnsavedChanges}");

                // Chamar OnViewUnloadingAsync para verificar e exibir notificação
                bool shouldCancel = await editableViewModel.OnViewUnloadingAsync();

                if (shouldCancel)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ NavigationService.GoBack() - Navegação cancelada pelo usuário");
                    return; // Cancelar navegação
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ NavigationService.GoBack() - Navegação permitida");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ℹ️ NavigationService.GoBack() - ViewModel não implementa IEditableViewModel");
            }

            var previousPage = _navigationStack.Pop();
            _contentArea.Content = previousPage;

            // Se o ViewModel anterior implementa IRefreshable, recarregar dados automaticamente
            if (previousPage?.DataContext is IRefreshable refreshable)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔄 NavigationService.GoBack() - Chamando RefreshData() em {previousPage.DataContext.GetType().Name}");
                refreshable.RefreshData();
            }

            Navigated?.Invoke(this, previousPage);
        }

        public void ClearHistory()
        {
            _navigationStack.Clear();
        }

        public void NavigateToHome(UserControl homePage)
        {
            _navigationStack.Clear();
            _contentArea.Content = homePage;
            Navigated?.Invoke(this, homePage);
        }

        /// <summary>
        /// Define um resultado para ser recuperado após a navegação de volta.
        /// Usado para passar dados entre ViewModels sem persistência imediata.
        /// </summary>
        public void SetNavigationResult(object? result)
        {
            _navigationResult = result;
        }

        /// <summary>
        /// Obtém e limpa o resultado da navegação anterior.
        /// </summary>
        public T? GetNavigationResult<T>() where T : class
        {
            var result = _navigationResult as T;
            _navigationResult = null;
            return result;
        }

        /// <summary>
        /// Verifica se há um resultado de navegação pendente.
        /// </summary>
        public bool HasNavigationResult => _navigationResult != null;

        /// <summary>
        /// Navega para a tela de edição de estudo (placeholder para implementação futura)
        /// </summary>
        public void NavigateToEstudoEdit(Estudo estudo, bool isCiclico = false)
        {
            // TODO: Implementar navegação para edição de estudo com modo cíclico
            NotificationService.Instance.ShowInfo(
                "Em Desenvolvimento",
                $"Navegação para edição de estudo em desenvolvimento.\nAssunto: {estudo.Assunto?.Nome}\nModo Cíclico: {isCiclico}");
        }
    }
}
