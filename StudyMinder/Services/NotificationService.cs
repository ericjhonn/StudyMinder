using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using StudyMinder.ViewModels;
using StudyMinder.Views;

namespace StudyMinder.Services
{
    /// <summary>
    /// Enum para tipos de mensagem (Success, Error, Warning, Info)
    /// </summary>
    public enum MessageType
    {
        Success,
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// Enum para botões da MessageBox
    /// </summary>
    public enum MessageBoxButtons
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }

    /// <summary>
    /// Enum para resultado da MessageBox
    /// </summary>
    public enum ToastMessageBoxResult
    {
        None,
        Ok,
        Cancel,
        Yes,
        No
    }

    /// <summary>
    /// ViewModel para Toast Notifications
    /// </summary>
    public class ToastViewModel : ObservableObject
    {
        private string _title;
        private string _message;
        private MessageType _messageType;
        private int _durationMs;
        private Guid _id;

        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public MessageType MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }

        public int DurationMs
        {
            get => _durationMs;
            set => SetProperty(ref _durationMs, value);
        }

        public ToastViewModel(string title, string message, MessageType messageType = MessageType.Info, int durationMs = 5000)
        {
            Id = Guid.NewGuid();
            Title = title;
            Message = message;
            MessageType = messageType;
            DurationMs = durationMs;
        }
    }

    /// <summary>
    /// Serviço Singleton para gerenciar notificações (Toasts e MessageBox)
    /// Implementa INotificationService para injeção de dependência
    /// </summary>
    public class NotificationService : INotificationService
    {
        private static NotificationService _instance;
        private static readonly object _lockObject = new object();
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _toastCancellationTokens;
        private NotificationSettings _settings;

        public ObservableCollection<ToastViewModel> Toasts { get; }
        public NotificationSettings Settings => _settings;

        private NotificationService()
        {
            Toasts = new ObservableCollection<ToastViewModel>();
            _toastCancellationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _settings = new NotificationSettings();
        }

        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new NotificationService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Exibe um Toast Notification (não bloqueante) com CancellationToken
        /// </summary>
        public void ShowToast(string title, string message, MessageType messageType = MessageType.Info, int? durationMs = null)
        {
            if (!_settings.EnableToasts) return;

            var duration = durationMs ?? _settings.DefaultDuration;
            var toast = new ToastViewModel(title, message, messageType, duration);
            var cts = new CancellationTokenSource();

            System.Diagnostics.Debug.WriteLine($"[Toast] Exibindo toast '{title}' com duração de {duration}ms");

            // Limitar número de toasts visíveis
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Toasts.Count >= _settings.MaxToasts)
                {
                    var oldestToast = Toasts.FirstOrDefault();
                    if (oldestToast != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Toast] Removendo toast antigo (limite atingido)");
                        Toasts.Remove(oldestToast);
                        if (_toastCancellationTokens.TryRemove(oldestToast.Id, out var oldCts))
                        {
                            oldCts.Cancel();
                        }
                    }
                }

                Toasts.Add(toast);
                System.Diagnostics.Debug.WriteLine($"[Toast] Toast adicionado. Total na tela: {Toasts.Count}");
            });

            // Adicionar CancellationToken ao dicionário
            _toastCancellationTokens[toast.Id] = cts;

            // Remover toast após duração especificada
            _ = Task.Delay(duration).ContinueWith(t =>
            {
                System.Diagnostics.Debug.WriteLine($"[Toast] Task.Delay completado para '{title}'. IsCanceled: {t.IsCanceled}, CancellationRequested: {cts.IsCancellationRequested}");
                
                // Verificar se o toast não foi cancelado manualmente
                if (!cts.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"[Toast] Removendo toast '{title}' após timeout");
                    
                    // Usar BeginInvoke para evitar deadlock e garantir execução
                    try
                    {
                        if (Application.Current?.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    if (Toasts.Contains(toast))
                                    {
                                        Toasts.Remove(toast);
                                        System.Diagnostics.Debug.WriteLine($"[Toast] Toast '{title}' removido com sucesso. Total na tela: {Toasts.Count}");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[Toast] Toast '{title}' não encontrado na coleção");
                                    }
                                    _toastCancellationTokens.TryRemove(toast.Id, out _);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Toast Removal] Erro ao remover toast '{title}': {ex.Message}");
                                }
                            });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Toast Removal] Dispatcher não disponível para remover toast '{title}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Toast Removal] Erro ao invocar Dispatcher para '{title}': {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Toast] Toast '{title}' foi cancelado manualmente");
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Exibe um Toast de Sucesso
        /// </summary>
        public void ShowSuccess(string title, string message, int? durationMs = null)
        {
            ShowToast(title, message, MessageType.Success, durationMs);
        }

        /// <summary>
        /// Exibe um Toast de Erro
        /// </summary>
        public void ShowError(string title, string message, int? durationMs = null)
        {
            ShowToast(title, message, MessageType.Error, durationMs ?? _settings.DefaultDuration + 1000);
        }

        /// <summary>
        /// Exibe um Toast de Aviso
        /// </summary>
        public void ShowWarning(string title, string message, int? durationMs = null)
        {
            ShowToast(title, message, MessageType.Warning, durationMs ?? _settings.DefaultDuration + 500);
        }

        /// <summary>
        /// Exibe um Toast de Informação
        /// </summary>
        public void ShowInfo(string title, string message, int? durationMs = null)
        {
            ShowToast(title, message, MessageType.Info, durationMs);
        }

        /// <summary>
        /// Exibe uma MessageBox Modal (bloqueante) usando janela customizada.
        /// Garante execução na UI thread e tratamento seguro de Owner / fallback nativo.
        /// </summary>
        public ToastMessageBoxResult ShowMessageBox(string title, string message, MessageType messageType = MessageType.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowMessageBox] Iniciando - Thread: {Thread.CurrentThread.ManagedThreadId}");

            // Se Application.Current não estiver disponível, usar MessageBox nativo diretamente
            if (Application.Current == null || Application.Current.Dispatcher == null)
            {
                System.Diagnostics.Debug.WriteLine("[ShowMessageBox] Application.Current ou Dispatcher nulo - usando MessageBox nativo");
                return ShowMessageBoxNative(title, message, messageType, buttons);
            }

            try
            {
                // Verificar se estamos na UI thread
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    System.Diagnostics.Debug.WriteLine("[ShowMessageBox] Não está na UI thread - invocando via Dispatcher");
                    return Application.Current.Dispatcher.Invoke(() =>
                    {
                        return ShowCustomMessageBoxInternal(title, message, messageType, buttons);
                    });
                }

                System.Diagnostics.Debug.WriteLine("[ShowMessageBox] Já está na UI thread - chamando ShowCustomMessageBoxInternal diretamente");
                return ShowCustomMessageBoxInternal(title, message, messageType, buttons);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowMessageBox] Erro ao exibir MessageBox customizada: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowMessageBox] StackTrace: {ex.StackTrace}");

                // Fallback confiável para MessageBox nativo do WPF
                return ShowMessageBoxNative(title, message, messageType, buttons);
            }
        }

        /// <summary>
        /// Implementação interna da MessageBox customizada (executada SEMPRE na UI thread).
        /// </summary>
        private ToastMessageBoxResult ShowCustomMessageBoxInternal(string title, string message, MessageType messageType, MessageBoxButtons buttons)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowCustomMessageBoxInternal] Iniciando - Thread: {Thread.CurrentThread.ManagedThreadId}");

            var viewModel = new MessageBoxViewModel(title, message, messageType, buttons);
            var window = new CustomMessageBoxWindow
            {
                DataContext = viewModel,
                ShowInTaskbar = false,
                Width = 450,
                SizeToContent = SizeToContent.Height
            };

            // Definir Owner de forma segura
            var app = Application.Current;
            if (app?.MainWindow != null && app.MainWindow.IsLoaded)
            {
                window.Owner = app.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                System.Diagnostics.Debug.WriteLine("[ShowCustomMessageBoxInternal] Owner definido como MainWindow (CenterOwner)");
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                System.Diagnostics.Debug.WriteLine("[ShowCustomMessageBoxInternal] MainWindow indisponível - usando CenterScreen sem Owner");
            }

            bool? dialogResult = null;

            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowCustomMessageBoxInternal] Chamando ShowDialog()");
                dialogResult = window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowCustomMessageBoxInternal] Erro em ShowDialog: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowCustomMessageBoxInternal] StackTrace: {ex.StackTrace}");
                throw; // Deixa o caller decidir o fallback
            }

            // O resultado principal vem do ViewModel
            var result = viewModel.Result;

            // Se por algum motivo nenhum botão definir o Result, inferir a partir do DialogResult
            if (result == ToastMessageBoxResult.None)
            {
                result = dialogResult == true
                    ? ToastMessageBoxResult.Ok
                    : ToastMessageBoxResult.Cancel;
            }

            System.Diagnostics.Debug.WriteLine($"[ShowCustomMessageBoxInternal] ShowDialog() retornou: {dialogResult}, Result: {result}");

            return result;
        }

        /// <summary>
        /// Implementação nativa do MessageBox (confiável e testada)
        /// </summary>
        private ToastMessageBoxResult ShowMessageBoxNative(string title, string message, MessageType messageType, MessageBoxButtons buttons)
        {
            System.Diagnostics.Debug.WriteLine("[ShowMessageBoxNative] Usando MessageBox nativo do WPF");
            
            MessageBoxButton nativeButton = MessageBoxButton.OK;
            MessageBoxImage nativeIcon = MessageBoxImage.Information;
            
            // Configurar botões
            switch (buttons)
            {
                case MessageBoxButtons.Ok:
                    nativeButton = MessageBoxButton.OK;
                    break;
                case MessageBoxButtons.OkCancel:
                    nativeButton = MessageBoxButton.OKCancel;
                    break;
                case MessageBoxButtons.YesNo:
                    nativeButton = MessageBoxButton.YesNo;
                    break;
                case MessageBoxButtons.YesNoCancel:
                    nativeButton = MessageBoxButton.YesNoCancel;
                    break;
            }
            
            // Configurar ícone
            switch (messageType)
            {
                case MessageType.Success:
                    nativeIcon = MessageBoxImage.Information;
                    break;
                case MessageType.Error:
                    nativeIcon = MessageBoxImage.Error;
                    break;
                case MessageType.Warning:
                    nativeIcon = MessageBoxImage.Warning;
                    break;
                case MessageType.Info:
                    nativeIcon = MessageBoxImage.Information;
                    break;
            }
            
            // Exibir MessageBox nativo e converter resultado
            var nativeResult = MessageBox.Show(message, title, nativeButton, nativeIcon);
            
            var result = nativeResult switch
            {
                MessageBoxResult.OK => ToastMessageBoxResult.Ok,
                MessageBoxResult.Yes => ToastMessageBoxResult.Yes,
                MessageBoxResult.No => ToastMessageBoxResult.No,
                MessageBoxResult.Cancel => ToastMessageBoxResult.Cancel,
                _ => ToastMessageBoxResult.Cancel
            };
            
            System.Diagnostics.Debug.WriteLine($"[ShowMessageBoxNative] MessageBox retornou: {nativeResult}, Convertido para: {result}");
            
            return result;
        }

        /// <summary>
        /// Exibe uma MessageBox de Confirmação (Sim/Não)
        /// </summary>
        public ToastMessageBoxResult ShowConfirmation(string title, string message)
        {
            return ShowMessageBox(title, message, MessageType.Warning, MessageBoxButtons.YesNo);
        }

        /// <summary>
        /// Exibe uma MessageBox de Erro
        /// </summary>
        public void ShowErrorBox(string title, string message)
        {
            ShowMessageBox(title, message, MessageType.Error, MessageBoxButtons.Ok);
        }

        /// <summary>
        /// Exibe uma MessageBox de Sucesso
        /// </summary>
        public void ShowSuccessBox(string title, string message)
        {
            ShowMessageBox(title, message, MessageType.Success, MessageBoxButtons.Ok);
        }

        /// <summary>
        /// Limpa todos os Toasts ativos
        /// </summary>
        public void ClearAllToasts()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Cancelar todos os CancellationTokenSources
                foreach (var cts in _toastCancellationTokens.Values)
                {
                    cts.Cancel();
                }
                _toastCancellationTokens.Clear();

                // Limpar coleção de toasts
                Toasts.Clear();
            });
        }

        /// <summary>
        /// Atualiza as configurações do serviço
        /// </summary>
        public void SetSettings(NotificationSettings settings)
        {
            _settings = settings ?? new NotificationSettings();
        }
    }
}
