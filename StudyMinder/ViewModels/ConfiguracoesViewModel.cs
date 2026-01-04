using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace StudyMinder.ViewModels
{
    public partial class ConfiguracoesViewModel : BaseViewModel, IEditableViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IThemeManager _themeManager;
        private readonly IPomodoroTimerService _pomodoroService;
        private readonly INotificationService _notificationService;
        private readonly IBackupService _backupService;
        private readonly StudyMinderContext _context;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private AppSettings _settings;

        // Aparência
        [ObservableProperty]
        private ObservableCollection<string> _availableThemes;

        [ObservableProperty]
        private ObservableCollection<string> _availableLayouts;

        // Backup
        [ObservableProperty]
        private ObservableCollection<string> _availableBackups;

        [ObservableProperty]
        private string? _selectedBackup;

        
        public ConfiguracoesViewModel(
            IConfigurationService configurationService,
            IThemeManager themeManager,
            IPomodoroTimerService pomodoroService,
            INotificationService notificationService,
            IBackupService backupService,
            StudyMinderContext context)
        {
            Title = "Configurações";
            
            _configurationService = configurationService;
            _themeManager = themeManager;
            _pomodoroService = pomodoroService;
            _notificationService = notificationService;
            _backupService = backupService;
            _context = context;

            _settings = _configurationService.Settings;
            
            _availableThemes = new ObservableCollection<string> { "Light", "Dark", "System" };
            _availableLayouts = new ObservableCollection<string> { "List", "Cards" };
            _availableBackups = new ObservableCollection<string>();

            // Subscrever aos eventos
            _configurationService.SettingsChanged += OnSettingsChanged;
            _backupService.BackupCreated += OnBackupCreated;
            _backupService.BackupError += OnBackupError;

            // Carregar dados iniciais
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await _configurationService.LoadAsync();
                await LoadAvailableBackupsAsync();
                
                // Sincronizar configurações com os serviços
                SyncServicesWithSettings();
            }
            catch (Exception ex)
            {
                // Log do erro
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar dados: {ex.Message}");
            }
        }

        private async Task LoadAvailableBackupsAsync()
        {
            try
            {
                var backups = await _backupService.GetAvailableBackupsAsync();
                AvailableBackups.Clear();
                
                foreach (var backup in backups)
                {
                    var fileName = Path.GetFileName(backup);
                    AvailableBackups.Add(fileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar backups: {ex.Message}");
            }
        }

        private void SyncServicesWithSettings()
        {
            // Sincronizar tema
            _themeManager.SetTheme(Settings.Appearance.Theme);
            
            // Sincronizar Pomodoro
            _pomodoroService.IsEnabled = Settings.Study.PomodoroEnabled;
            _pomodoroService.FocusMinutes = Settings.Study.PomodoroFocusMinutes;
            _pomodoroService.BreakMinutes = Settings.Study.PomodoroBreakMinutes;
            
            // Sincronizar notificações
            // _notificationService.IsEnabled não está disponível na interface - removido
            
            // Sincronizar backup
            _backupService.AutoBackupEnabled = Settings.Database.AutoBackupEnabled;
            _backupService.BackupFrequencyDays = Settings.Database.BackupFrequencyDays;
            _backupService.MaxBackupsToKeep = Settings.Database.MaxBackupsToKeep;
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Iniciando SalvarAsync");
                IsSaving = true;

                // Validar e corrigir configurações antes de salvar
                if (Settings.Study.PomodoroFocusMinutes < 1) Settings.Study.PomodoroFocusMinutes = 25;
                if (Settings.Study.PomodoroBreakMinutes < 1) Settings.Study.PomodoroBreakMinutes = 5;
                if (Settings.Database.BackupFrequencyDays < 1) Settings.Database.BackupFrequencyDays = 7;
                if (Settings.Database.MaxBackupsToKeep < 0) Settings.Database.MaxBackupsToKeep = 10;

                // Sincronizar configurações com os serviços
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Sincronizando serviços com settings");
                SyncServicesWithSettings();
                
                // Salvar configurações
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Chamando ConfigurationService.SaveAsync()");
                await _configurationService.SaveAsync();
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] SaveAsync() completado com sucesso");
                
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Exibindo notificação de sucesso");
                _notificationService.ShowSuccess("Configurações Salvas", "Configurações salvas com sucesso!");
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Notificação exibida");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfiguracoesViewModel] ERRO em SalvarAsync: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ConfiguracoesViewModel] StackTrace: {ex.StackTrace}");
                _notificationService.ShowError("Erro ao Salvar", $"Erro ao salvar configurações: {ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[ConfiguracoesViewModel] Finalizando SalvarAsync");
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancelar()
        {
            // Recarregar configurações originais
            _ = _configurationService.LoadAsync();
        }

        [RelayCommand]
        private async Task ResetarConfiguracoes()
        {
            var result = _notificationService.ShowConfirmation(
                "Confirmar Reset",
                "Tem certeza que deseja restaurar todas as configurações para os valores padrão?");

            if (result == ToastMessageBoxResult.Yes)
            {
                try
                {
                    await _configurationService.ResetToDefaultsAsync();
                    _notificationService.ShowSuccess("Configurações Restauradas", "Configurações restauradas com sucesso!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Erro ao Restaurar", $"Erro ao restaurar configurações: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task CriarBackup()
        {
            try
            {
                var backupPath = await _backupService.CreateBackupAsync();
                var fileName = Path.GetFileName(backupPath);
                
                _notificationService.ShowSuccess("Backup Criado", $"Backup criado com sucesso!\nArquivo: {fileName}");
                
                await LoadAvailableBackupsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Criar Backup", $"Erro ao criar backup: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RestaurarBackup()
        {
            if (string.IsNullOrEmpty(SelectedBackup))
            {
                _notificationService.ShowWarning("Aviso", "Selecione um backup para restaurar.");
                return;
            }

            var result = _notificationService.ShowConfirmation(
                "Confirmar Restauração",
                $"Tem certeza que deseja restaurar o backup '{SelectedBackup}'?\n\nEsta ação substituirá todos os dados atuais!");

            if (result == ToastMessageBoxResult.Yes)
            {
                try
                {
                    var backupPath = Path.Combine(_backupService.BackupDirectory, SelectedBackup);
                    var success = await _backupService.RestoreBackupAsync(backupPath);

                    if (success)
                    {
                        _notificationService.ShowSuccess("Restauração Concluída", "O aplicativo será reiniciado para carregar os dados restaurados.");

                        // Mesma lógica que você já usou no ExcluirTodosDados
                        var processPath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            System.Diagnostics.Process.Start(processPath);
                        }
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Erro ao Restaurar", $"Erro ao restaurar backup: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task ExportarDados()
        {
            await Task.Run(() =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                        {
                            Filter = "Arquivo CSV (*.csv)|*.csv|Arquivo Excel (*.xlsx)|*.xlsx",
                            DefaultExt = "csv",
                            FileName = $"StudyMinder_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                        };

                        if (saveFileDialog.ShowDialog() == true)
                        {
                            // Implementar exportação de dados
                            // Por enquanto, apenas mostrar mensagem
                            _notificationService.ShowInfo("Em Desenvolvimento", "Funcionalidade de exportação será implementada em breve.");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _notificationService.ShowError("Erro ao Exportar", $"Erro ao exportar dados: {ex.Message}");
                    });
                }
            });
        }

        [RelayCommand]
        private async Task ImportarDados()
        {
            await Task.Run(() =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var openFileDialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "Arquivos Suportados (*.csv;*.xlsx;*.xls)|*.csv;*.xlsx;*.xls|Arquivo CSV (*.csv)|*.csv|Arquivo Excel (*.xlsx;*.xls)|*.xlsx;*.xls",
                            Multiselect = false
                        };

                        if (openFileDialog.ShowDialog() == true)
                        {
                            // Implementar importação de dados
                            // Por enquanto, apenas mostrar mensagem
                            _notificationService.ShowInfo("Em Desenvolvimento", "Funcionalidade de importação será implementada em breve.");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _notificationService.ShowError("Erro ao Importar", $"Erro ao importar dados: {ex.Message}");
                    });
                }
            });
        }

        [RelayCommand]
        private async Task ExcluirTodosDados()
        {
            if (_context == null)
            {
                _notificationService.ShowError("Erro", "Erro: Contexto do banco de dados não está disponível.");
                return;
            }

            var result = _notificationService.ShowConfirmation(
                "Excluir Todos os Dados",
                "⚠️ ATENÇÃO!\n\nEsta ação excluirá PERMANENTEMENTE todos os dados do banco de dados:\n" +
                "• Disciplinas\n" +
                "• Assuntos\n" +
                "• Estudos\n" +
                "• Revisões\n" +
                "• Editais\n" +
                "• Cronogramas\n\n" +
                "Dados universais do sistema (Escolaridades, Status de Edital, Tipos de Estudo) serão preservados.\n\n" +
                "Esta ação é IRREVERSÍVEL!\n\n" +
                "Tem certeza que deseja continuar?");

            if (result == ToastMessageBoxResult.Yes)
            {
                // Confirmação adicional
                var confirmacao = _notificationService.ShowMessageBox(
                    "Confirmação Final",
                    "Tem ABSOLUTA certeza? Digite 'SIM' para confirmar a exclusão permanente de todos os dados.",
                    MessageType.Warning,
                    MessageBoxButtons.OkCancel);

                if (confirmacao == ToastMessageBoxResult.Ok)
                {
                    try
                    {
                        IsSaving = true;

                        // Excluir dados do usuário em ordem de dependência
                        // PRESERVANDO tabelas universais: Escolaridades, StatusesEdital, TiposEstudo
                        await Task.Run(async () =>
                        {
                            // Usar ExecuteDeleteAsync para não carregar dados na memória
                            // Isso evita problemas com propriedades calculadas [NotMapped]
                                                        
                            // Excluir estudos (antes de excluir assuntos, por FK)
                            await _context.Estudos.ExecuteDeleteAsync();

                            // Excluir cronogramas de editais (antes de excluir editais, por FK)
                            await _context.EditalCronograma.ExecuteDeleteAsync();

                            // Excluir assuntos de editais (antes de excluir editais e assuntos, por FK)
                            await _context.EditalAssuntos.ExecuteDeleteAsync();

                            // Excluir editais
                            await _context.Editais.ExecuteDeleteAsync();

                            // Excluir assuntos (depois de excluir estudos, por FK)
                            await _context.Assuntos.ExecuteDeleteAsync();

                            // Excluir disciplinas (depois de excluir assuntos, por FK)
                            await _context.Disciplinas.ExecuteDeleteAsync();

                            // NÃO excluir tabelas universais:
                            // - Escolaridades (dados universais do sistema)
                            // - StatusesEdital (dados universais do sistema)
                            // - TiposEstudo (dados universais do sistema)
                            // - FasesEdital (dados universais do sistema)
                        });

                        _notificationService.ShowSuccess(
                            "Exclusão Concluída",
                            "Todos os dados foram excluídos com sucesso!\n\n" +
                            "Dados universais do sistema foram preservados.\n\n" +
                            "O aplicativo será reiniciado para aplicar as mudanças.");

                        // Reiniciar a aplicação usando Environment.ProcessPath (funciona em single-file)
                        var processPath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            System.Diagnostics.Process.Start(processPath);
                        }
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError(
                            "Erro ao Excluir",
                            $"Erro ao excluir dados: {ex.Message}");
                    }
                    finally
                    {
                        IsSaving = false;
                    }
                }
            }
        }

        [RelayCommand]
        private async Task TestarNotificacao()
        {
            try
            {
                _notificationService.ShowInfo(
                    "Teste de Notificação",
                    "Esta é uma notificação de teste do StudyMinder!");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Testar", $"Erro ao testar notificação: {ex.Message}");
            }
        }

        private void OnSettingsChanged(object? sender, AppSettings settings)
        {
            Settings = settings;
        }

        private async void OnBackupCreated(object? sender, BackupEventArgs e)
        {
            if (e.Success)
            {
                await LoadAvailableBackupsAsync();
            }
        }

        private void OnBackupError(object? sender, BackupEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _notificationService.ShowError("Erro de Backup", $"Erro no backup: {e.ErrorMessage}");
                });
            }
        }

        /// <summary>
        /// Propriedade para rastrear se há alterações não salvas
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                if (Settings == null)
                    return false;

                // Comparar configurações atuais com as originais
                return Settings.Appearance.Theme != _configurationService.Settings.Appearance.Theme ||
                       Settings.Study.PomodoroEnabled != _configurationService.Settings.Study.PomodoroEnabled ||
                       Settings.Study.PomodoroFocusMinutes != _configurationService.Settings.Study.PomodoroFocusMinutes ||
                       Settings.Study.PomodoroBreakMinutes != _configurationService.Settings.Study.PomodoroBreakMinutes ||
                       Settings.Database.AutoBackupEnabled != _configurationService.Settings.Database.AutoBackupEnabled ||
                       Settings.Database.BackupFrequencyDays != _configurationService.Settings.Database.BackupFrequencyDays ||
                       Settings.Database.MaxBackupsToKeep != _configurationService.Settings.Database.MaxBackupsToKeep;
            }
        }

        /// <summary>
        /// Implementação de IEditableViewModel - Chamado quando a view está sendo descarregada
        /// Retorna true se deve cancelar a navegação, false caso contrário
        /// </summary>
        public async Task<bool> OnViewUnloadingAsync()
        {
            if (!HasUnsavedChanges)
                return false;

            var resultado = _notificationService.ShowConfirmation(
                "Alterações Não Salvas",
                "Você tem alterações não salvas. Deseja descartá-las?");

            return resultado != ToastMessageBoxResult.Yes;
        }
    }
}
