using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StudyMinder.Services;
using StudyMinder.Views;
using StudyMinder.ViewModels;
using StudyMinder.Data;

namespace StudyMinder;

public partial class App : Application
{
    private static IThemeManager? _themeManager;
    private static IConfigurationService? _configurationService;

    public static IThemeManager ThemeManager => _themeManager ??= new ThemeManager();
    public static IConfigurationService ConfigurationService => _configurationService ??= new ConfigurationService();

    public IServiceProvider ServiceProvider { get; private set; }

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Banco de Dados
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var dbPath = Path.Combine(exeDir, "StudyMinder.db");

        if (!File.Exists(dbPath))
        {
            var parentDir = Directory.GetParent(exeDir)?.FullName;
            if (!string.IsNullOrEmpty(parentDir))
            {
                var altPath = Path.Combine(parentDir, "StudyMinder.db");
                if (File.Exists(altPath)) dbPath = altPath;
            }
        }

        services.AddTransient<StudyMinderContext>(provider =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<StudyMinderContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new StudyMinderContext(optionsBuilder.Options);
        });

        // 2. Serviços
        services.AddTransient<AuditoriaService>();
        services.AddTransient<EditalService>();

        // ADICIONADO: Serviço necessário para o botão de Adicionar ao Ciclo
        services.AddTransient<CicloEstudoService>();

        services.AddSingleton<INotificationService>(provider => NotificationService.Instance);
        services.AddTransient<ComparadorEditaisService>();

        // 3. ViewModels
        services.AddTransient<ComparadorEditaisViewModel>();
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private void LogUnhandledException(Exception exception, string source)
    {
        string message = $"Ocorreu um erro inesperado ({source}):\n\n{exception.Message}\n\nStackTrace:\n{exception.StackTrace}";
        if (exception.InnerException != null)
        {
            message += $"\n\nErro Interno:\n{exception.InnerException.Message}";
        }

        if (Current?.MainWindow != null && Current.MainWindow.IsLoaded)
            MessageBox.Show(Current.MainWindow, message, "Erro Crítico - StudyMinder", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            MessageBox.Show(message, "Erro Crítico - StudyMinder", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var splashScreen = new Views.SplashScreen();
        splashScreen.Show();

        try
        {
            await Task.Run(async () =>
            {
                splashScreen.Dispatcher.Invoke(() => splashScreen.UpdateStatus("Carregando temas..."));
                var themeTestResult = ThemeManager.TestThemeLoading();

                splashScreen.Dispatcher.Invoke(() => splashScreen.UpdateStatus("Carregando configurações..."));
                await ConfigurationService.LoadAsync();

                splashScreen.Dispatcher.Invoke(() => splashScreen.UpdateStatus("Aplicando tema..."));
                await Task.Delay(2000);
                splashScreen.Dispatcher.Invoke(() => ThemeManager.SetTheme(ConfigurationService.Settings.Appearance.Theme));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações: {ex.Message}");
            ThemeManager.SetTheme(AppTheme.System);
        }
        finally
        {
            splashScreen.Dispatcher.Invoke(() => splashScreen.UpdateStatus("Carregando dashboard..."));
            try { await Task.Delay(200); } catch { }

            splashScreen.CloseSplash();
            base.OnStartup(e);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            ConfigurationService?.SaveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações: {ex.Message}");
        }
        base.OnExit(e);
    }
}