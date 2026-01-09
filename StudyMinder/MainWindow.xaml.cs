using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Navigation;
using StudyMinder.Services;
using StudyMinder.Utils;
using StudyMinder.ViewModels;
using StudyMinder.Views;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.IconPacks;
using static StudyMinder.Services.NotificationService;
using Path = System.IO.Path;

namespace StudyMinder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;
    private readonly StudyMinderContext _context;
    private readonly IConfigurationService _configurationService;
    private readonly ViewHome _homeView;
    private readonly ViewDisciplinas _disciplinasView;
    private readonly ViewEstudos _estudosView;
    private readonly ViewCicloEstudo _cicloEstudoView;
    private readonly ViewEditais _editaisView;
    private readonly ViewComparadorEditais _comparadorView;
    private readonly ViewCalendario _calendarioView;
    private readonly ViewGraficos _graficosView;
    private readonly ViewSobre _sobreView;
    private readonly ViewConfiguracoes _configuracoesView;
    private readonly ViewRevisoesClassicas _revisoesClassicasView;
    private readonly ViewRevisoes42 _revisoes42View;
    private readonly ViewRevisoesCiclicas _revisoesCiclicasView;
    private readonly IBackupService _backupService;
    private readonly DisciplinaService _disciplinaService;
    private readonly AssuntoService _assuntoService;
    private readonly AuditoriaService _auditoriaService;
    private readonly EstudoService _estudoService;
    private readonly CicloEstudoService _cicloEstudoService;
    private readonly TipoEstudoService _tipoEstudoService;
    private readonly PomodoroTimerService _pomodoroService;
    private readonly EditalService _editalService;
    private readonly EstudoNotificacaoService _estudoNotificacaoService;
    private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
    private readonly RevisaoService _revisaoService;
    private readonly RevisaoCicloAtivoService _revisaoCicloAtivoService;
    private readonly EditalCronogramaService _editalCronogramaService;
    private readonly EditalCronogramaNotificacaoService _editalCronogramaNotificacaoService;
    private Button? _activeButton;
    private Button? _activeSubmenuButton;
    private Button? _parentMenuButton; // Rastrear o botão pai do submenu
    private bool _revisoesExpanded = false;
    private bool _isSidebarExpanded = true;

    public MainWindow()
    {
        InitializeComponent();

        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var dbPath = Path.Combine(exeDir, "StudyMinder.db");
        if (!File.Exists(dbPath))
        {
            dbPath = Path.Combine(Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
        }

        var optionsBuilder = new DbContextOptionsBuilder<StudyMinderContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        _context = new StudyMinderContext(optionsBuilder.Options);
        _context.Database.EnsureCreated();
        
        _configurationService = new ConfigurationService();
        _backupService = new BackupService(_context, _configurationService);
        _auditoriaService = new AuditoriaService();
        _disciplinaService = new DisciplinaService(_context, _auditoriaService);
        _assuntoService = new AssuntoService(_context, _auditoriaService, _disciplinaService);
        _estudoService = new EstudoService(_context, _auditoriaService);
        _cicloEstudoService = new CicloEstudoService(_context);
        _tipoEstudoService = new TipoEstudoService(_context, _auditoriaService);
        _pomodoroService = new PomodoroTimerService();
        _editalService = new EditalService(_context, _auditoriaService);
        _estudoNotificacaoService = new EstudoNotificacaoService();
        _revisaoNotificacaoService = new RevisaoNotificacaoService();
        _revisaoService = new RevisaoService(_context, _auditoriaService);
        _revisaoCicloAtivoService = new RevisaoCicloAtivoService(_context, _auditoriaService);
        _editalCronogramaService = new EditalCronogramaService(_context, _auditoriaService);
        _editalCronogramaNotificacaoService = new EditalCronogramaNotificacaoService();
        
        _navigationService = new NavigationService(ContentArea, _context);
        _homeView = new ViewHome();
        _disciplinasView = new ViewDisciplinas();
        _estudosView = new ViewEstudos();
        _cicloEstudoView = new ViewCicloEstudo();
        _editaisView = new ViewEditais();
        _comparadorView = new ViewComparadorEditais();
        _calendarioView = new ViewCalendario();
        _graficosView = new ViewGraficos();
        _sobreView = new ViewSobre
        {
            DataContext = new SobreViewModel()
        };
        _configuracoesView = new ViewConfiguracoes();
        _revisoesClassicasView = new ViewRevisoesClassicas();
        _revisoes42View = new ViewRevisoes42();
        _revisoesCiclicasView = new ViewRevisoesCiclicas();
        
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void btnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void btnTheme_Click(object sender, RoutedEventArgs e)
    {
        // Alternar entre Light e Dark
        var currentTheme = App.ThemeManager.CurrentTheme;
        var newTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        
        // Aplicar novo tema
        App.ThemeManager.SetTheme(newTheme);
        
        // Salvar preferência
        _configurationService.Settings.Appearance.Theme = newTheme.ToString();
        _configurationService.SaveAsync();
        
        System.Diagnostics.Debug.WriteLine($"[Theme] Tema alterado para: {newTheme}");
    }

    private void btnNotify_Click(object sender, RoutedEventArgs e)
    {
        // Implementation for notification center can be added here
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _backupService.CheckAndRunAutoBackupAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Backup] Falha no auto backup: {ex.Message}");
            // Opcional: notificar o usuário com um Toast ou uma mensagem discreta
        }

        await LoadWindowSettings();

        _activeButton = BtnHome;
        _navigationService.NavigateToHome(_homeView);
        
        var notificationService = NotificationService.Instance;
        var estudoTransactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        
        var homeViewModel = new HomeViewModel(
            _context,
            _estudoService,
            _navigationService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            estudoTransactionService,
            _revisaoService,
            _revisaoNotificacaoService,
            notificationService,
            _configurationService);
        
        _homeView.DataContext = homeViewModel;
        
        await homeViewModel.CarregarDadosAsync();
    }

    private async Task LoadWindowSettings()
    {
        await _configurationService.LoadAsync();
        var windowSettings = _configurationService.Settings.Window;

        // Basic validation to prevent opening the window off-screen
        if (windowSettings.Width > 100 && windowSettings.Height > 100)
        {
            Width = windowSettings.Width;
            Height = windowSettings.Height;

            // Ensure window is not positioned completely off-screen
            var screen = System.Windows.SystemParameters.WorkArea;
            Left = Math.Max(screen.Left, Math.Min(windowSettings.Left, screen.Right - Width));
            Top = Math.Max(screen.Top, Math.Min(windowSettings.Top, screen.Bottom - Height));
        }

        if (windowSettings.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private async void SaveWindowSettings()
    {
        var windowSettings = _configurationService.Settings.Window;

        // Only save dimensions if the window is in a normal state
        if (WindowState == WindowState.Normal)
        {
            windowSettings.Width = (int)Width;
            windowSettings.Height = (int)Height;
            windowSettings.Left = (int)Left;
            windowSettings.Top = (int)Top;
        }
        else
        {
            // If maximized or minimized, save the restore bounds
            windowSettings.Width = (int)RestoreBounds.Width;
            windowSettings.Height = (int)RestoreBounds.Height;
            windowSettings.Left = (int)RestoreBounds.Left;
            windowSettings.Top = (int)RestoreBounds.Top;
        }

        windowSettings.Maximized = WindowState == WindowState.Maximized;

        await _configurationService.SaveAsync();
    }


    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowSettings();
        _context?.Dispose();
    }

    private void SetActiveButton(Button activeButton)
    {
        ClearActiveSubmenuButton();
        _parentMenuButton = null;
        
        if (_activeButton != null)
        {
            _activeButton.Style = FindResource("PrimarySidebarButtonStyle") as Style;
        }

        activeButton.Style = FindResource("ActiveSidebarButtonStyle") as Style;
        _activeButton = activeButton;
    }
    
    private void ClearActiveSubmenuButton()
    {
        if (_activeSubmenuButton != null)
        {
            _activeSubmenuButton.Style = FindResource("SecondarySidebarButtonStyle") as Style;
            _activeSubmenuButton = null;
        }
    }

    private void SetActiveSubmenuButton(Button activeSubmenuButton)
    {
        if (_activeSubmenuButton != null)
        {
            _activeSubmenuButton.Style = FindResource("SecondarySidebarButtonStyle") as Style;
        }
        if (_parentMenuButton != null)
        {
            _parentMenuButton.Style = FindResource("PrimarySidebarButtonStyle") as Style;
        }
        if (_activeButton != null && _activeButton.Name != "BtnRevisoes")
        {
            _activeButton.Style = FindResource("PrimarySidebarButtonStyle") as Style;
        }
        activeSubmenuButton.Style = FindResource("ActiveSubmenuButtonStyle") as Style;
        _activeSubmenuButton = activeSubmenuButton;
        _parentMenuButton = BtnRevisoes;
        _activeButton = null;
    }

    private async void BtnHome_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnHome);
        _navigationService.NavigateToHome(_homeView);
        if (_homeView.DataContext is HomeViewModel homeViewModel)
        {
            await homeViewModel.CarregarDadosAsync();
        }
    }

    private void BtnDisciplinas_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnDisciplinas);
        _navigationService.NavigateTo(_disciplinasView);
        var transactionService = new DisciplinaAssuntoTransactionService(_context, new AuditoriaService());
        var disciplinasViewModel = new DisciplinasViewModel(_disciplinaService, _assuntoService, transactionService, _navigationService, NotificationService.Instance, _configurationService);
        _disciplinasView.DataContext = disciplinasViewModel;
        if (disciplinasViewModel.LoadDisciplinasCommand.CanExecute(null))
        {
            disciplinasViewModel.LoadDisciplinasCommand.Execute(null);
        }
    }

    private void BtnEstudos_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnEstudos);
        _navigationService.NavigateTo(_estudosView);
        var transactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        var estudosViewModel = new EstudosViewModel(
            _estudoService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            transactionService,
            _pomodoroService,
            _navigationService,
            _estudoNotificacaoService,
            _revisaoService,
            NotificationService.Instance,
            _configurationService);
        _estudosView.DataContext = estudosViewModel;
    }

    private void BtnCicloEstudo_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnCicloEstudo);
        var cicloEstudoViewModel = new CicloEstudoViewModel(
            _cicloEstudoService,
            _navigationService,
            _estudoService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService),
            NotificationService.Instance,
            _configurationService,
            _revisaoService);
        _cicloEstudoView.DataContext = cicloEstudoViewModel;
        _navigationService.NavigateTo(_cicloEstudoView);
    }

    private void BtnRevisoes_Click(object sender, RoutedEventArgs e)
    {
        _revisoesExpanded = !_revisoesExpanded;
        if (RevisoesExpandIcon != null)
        {
            RevisoesExpandIcon.Kind = _revisoesExpanded ? PackIconMaterialKind.ChevronUp : PackIconMaterialKind.ChevronDown;
        }
        RevisoesSubmenuPanel.Visibility = _revisoesExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnEditais_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnEditais);
        _navigationService.NavigateTo(_editaisView);
        var editalTransactionService = new EditalTransactionService(_context, _auditoriaService);
        var editaisViewModel = new EditaisViewModel(_editalService, editalTransactionService, _navigationService, _revisaoNotificacaoService, NotificationService.Instance, _configurationService);
        _editaisView.DataContext = editaisViewModel;
        if (editaisViewModel.LoadEditaisCommand.CanExecute(null))
        {
            editaisViewModel.LoadEditaisCommand.Execute(null);
        }
    }

    private void BtnComparador_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnComparador);
        _navigationService.NavigateTo(_comparadorView);

        // Opcional: Se quiser forçar uma limpeza/atualização ao entrar na tela
        /* if (_comparadorView.DataContext is ComparadorEditaisViewModel vm)
        {
            vm.LimparSelecaoCommand.Execute(null);
        }
        */
    }
    private void BtnCalendario_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnCalendario);
        _navigationService.NavigateTo(_calendarioView);
        var estudoTransactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        var calendarioViewModel = new CalendarioViewModel(
            _context,
            _estudoNotificacaoService,
            _editalCronogramaNotificacaoService,
            _estudoService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            estudoTransactionService,
            _navigationService,
            _revisaoService,
            NotificationService.Instance,
            _configurationService);
        _calendarioView.DataContext = calendarioViewModel;
    }

    private void BtnGraficos_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnGraficos);
        _navigationService.NavigateTo(_graficosView);
        _graficosView.DataContext = new GraficosViewModel();
    }

    private void BtnSobre_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnSobre);
        _navigationService.NavigateTo(_sobreView);
    }

    private async void BtnRevisoesClassicas_Click(object sender, RoutedEventArgs e)
    {
        if (!_revisoesExpanded)
        {
            _revisoesExpanded = true;
            RevisoesExpandIcon.Kind = PackIconMaterialKind.ChevronUp;
            RevisoesSubmenuPanel.Visibility = Visibility.Visible;
        }
        
        SetActiveSubmenuButton(BtnRevisoesClassicas);
        _navigationService.NavigateTo(_revisoesClassicasView);
        var estudoTransactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        var revisoesClassicasViewModel = new RevisoesClassicasViewModel(
            _revisaoService,
            _estudoService,
            _navigationService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            estudoTransactionService,
            _revisaoNotificacaoService,
            NotificationService.Instance,
            _configurationService);
        _revisoesClassicasView.DataContext = revisoesClassicasViewModel;
        await revisoesClassicasViewModel.InitializeAsync();
    }

    private async void BtnRevisoes42_Click(object sender, RoutedEventArgs e)
    {
        if (!_revisoesExpanded)
        {
            _revisoesExpanded = true;
            RevisoesExpandIcon.Kind = PackIconMaterialKind.ChevronUp;
            RevisoesSubmenuPanel.Visibility = Visibility.Visible;
        }
        
        SetActiveSubmenuButton(BtnRevisoes42);
        _navigationService.NavigateTo(_revisoes42View);
        var estudoTransactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        var revisoes42ViewModel = new Revisoes42ViewModel(
            _revisaoService,
            _estudoService,
            _navigationService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            estudoTransactionService,
            _revisaoNotificacaoService,
            NotificationService.Instance,
            _configurationService);
        _revisoes42View.DataContext = revisoes42ViewModel;
        await revisoes42ViewModel.InitializeAsync();
    }

    private async void BtnRevisoesCiclicas_Click(object sender, RoutedEventArgs e)
    {
        if (!_revisoesExpanded)
        {
            _revisoesExpanded = true;
            RevisoesExpandIcon.Kind = PackIconMaterialKind.ChevronUp;
            RevisoesSubmenuPanel.Visibility = Visibility.Visible;
        }
        
        SetActiveSubmenuButton(BtnRevisoesCiclicas);
        _navigationService.NavigateTo(_revisoesCiclicasView);
        
        var estudoTransactionService = new EstudoTransactionService(_context, _auditoriaService, _revisaoNotificacaoService);
        var revisoesCiclicasViewModel = new RevisoesCiclicasViewModel(
            _revisaoService,
            _revisaoCicloAtivoService,
            _estudoService,
            _navigationService,
            _revisaoNotificacaoService,
            _tipoEstudoService,
            _assuntoService,
            _disciplinaService,
            estudoTransactionService,
            NotificationService.Instance,
            _configurationService);
        
        _revisoesCiclicasView.DataContext = revisoesCiclicasViewModel;
        await revisoesCiclicasViewModel.InitializeAsync();
    }

    private void BtnConfiguracoes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetActiveButton(BtnConfiguracoes);
            _navigationService.NavigateTo(_configuracoesView);
            
            var configuracoesViewModel = new ConfiguracoesViewModel(
                _configurationService,
                new ThemeManager(),
                _pomodoroService,
                NotificationService.Instance,
                _backupService,
                _context);
            
            _configuracoesView.DataContext = configuracoesViewModel;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar configurações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_isSidebarExpanded)
        {
            if (_revisoesExpanded)
            {
                _revisoesExpanded = false;
                RevisoesSubmenuPanel.Visibility = Visibility.Collapsed;
                if (RevisoesExpandIcon != null)
                {
                    RevisoesExpandIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.ChevronDown;
                }
            }
            Storyboard sb = (Storyboard)this.FindResource("CollapseSidebar");
            sb.Begin();
        }
        else
        {
            Storyboard sb = (Storyboard)this.FindResource("ExpandSidebar");
            sb.Begin();
        }

        _isSidebarExpanded = !_isSidebarExpanded;
    }
}
