using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudyMinder.ViewModels;
using System.Diagnostics;
using System.Windows.Forms;
using WForms = System.Windows.Forms;
using WControls = System.Windows.Controls;
using WInput = System.Windows.Input;
using System.IO;

namespace StudyMinder.Views
{
    /// <summary>
    /// Interaction logic for ViewEstudoEditar.xaml
    /// </summary>
    public partial class ViewEstudoEditar : WControls.UserControl
    {
        private bool _isUpdatingText = false;
        private Window? _parentWindow;
        private bool _isMinimized = false;
        private NotifyIcon? _notifyIcon;
        
        // Cache de ícones - reutilizar as mesmas instâncias
        private System.Drawing.Icon? _playIcon;
        private System.Drawing.Icon? _stopIcon;

        public ViewEstudoEditar()
        {
            InitializeComponent();
            Loaded += ViewEstudoEditar_Loaded;
            Unloaded += ViewEstudoEditar_Unloaded;
        }

        private void ViewEstudoEditar_Unloaded(object sender, RoutedEventArgs e)
        {
            // Limpar recursos do NotifyIcon
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            
            // Limpar cache de ícones
            _playIcon?.Dispose();
            _stopIcon?.Dispose();
            _playIcon = null;
            _stopIcon = null;
        }

        private void ViewEstudoEditar_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[Bandeja] ViewEstudoEditar_Loaded iniciado");
            
            // Obter a janela pai
            _parentWindow = Window.GetWindow(this);
            Debug.WriteLine($"[Bandeja] _parentWindow obtida: {(_parentWindow != null ? "Sucesso" : "Nula")}");
            
            // Configurar gerenciamento de bandeja quando a view é carregada
            if (DataContext is EditarEstudoViewModel viewModel)
            {
                Debug.WriteLine("[Bandeja] DataContext é EditarEstudoViewModel - configurando bandeja");
                
                // Conectar as ações de callback do ViewModel para minimização/restauração
                viewModel.OnSolicitarMinimizacao = MinimizarParaBandeja;
                viewModel.OnSolicitarRestauracao = RestaurarDaBandeja;
                
                CriarNotifyIcon(viewModel);
                ConfigurarGerenciamentoBandeja(viewModel);
            }
            else
            {
                Debug.WriteLine($"[Bandeja] DataContext não é EditarEstudoViewModel: {DataContext?.GetType().Name ?? "Nulo"}");
            }
        }

        private void CriarNotifyIcon(EditarEstudoViewModel viewModel)
        {
            if (_notifyIcon != null)
            {
                Debug.WriteLine("[Bandeja] NotifyIcon já existe, ignorando");
                return;
            }

            try
            {
                Debug.WriteLine("[Bandeja] Iniciando criação de NotifyIcon");
                
                // Carregar ambos os ícones uma única vez para reutilizar
                _playIcon = CarregarIconeDoRecurso("Play.ico");
                _stopIcon = CarregarIconeDoRecurso("Stop.ico");
                
                if (_playIcon != null && _stopIcon != null)
                {
                    Debug.WriteLine("[Bandeja] ✓ Ambos os ícones carregados em cache");
                    
                    _notifyIcon = new NotifyIcon
                    {
                        Icon = _playIcon,  // Iniciar com Play
                        Visible = true,    // Sempre visível desde o carregamento
                        Text = "StudyMinder - Timer - Clique para iniciar"
                    };

                    // Clique simples no ícone alterna o timer e minimiza (se configurado)
                    _notifyIcon.MouseClick += (s, e) =>
                    {
                        if (e.Button == System.Windows.Forms.MouseButtons.Left)
                        {
                            if (viewModel.AlternarTimerEMinimizarCommand.CanExecute(null))
                            {
                                viewModel.AlternarTimerEMinimizarCommand.Execute(null);
                            }
                        }
                    };

                    Debug.WriteLine("[Bandeja] ✓ NotifyIcon criado com sucesso");
                    Debug.WriteLine("[Bandeja] ✓ Ícone visível na bandeja");
                }
                else
                {
                    Debug.WriteLine($"[Bandeja] ✗ Falha ao carregar ícones - Play: {(_playIcon != null ? "OK" : "ERRO")}, Stop: {(_stopIcon != null ? "OK" : "ERRO")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Bandeja] ✗ Erro ao criar NotifyIcon: {ex.Message}");
                Debug.WriteLine($"[Bandeja] StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Carrega um ícone dos recursos embutidos da aplicação.
        /// Utiliza pack:// URI para acessar recursos compilados no assembly.
        /// </summary>
        private System.Drawing.Icon? CarregarIconeDoRecurso(string nomeArquivo)
        {
            try
            {
                // URI do tipo: pack://application:,,,/StudyMinder;component/Images/Play.ico
                //var uri = new Uri($"pack://application:,,,/StudyMinder;component/Images/{nomeArquivo}");
                var uri = new Uri($"/Images/{nomeArquivo}", UriKind.Relative); 
                Debug.WriteLine($"[Bandeja] Carregando recurso: {uri}");

                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                
                if (streamInfo != null && streamInfo.Stream != null)
                {
                    Debug.WriteLine($"[Bandeja] ✓ Stream obtido para {nomeArquivo}");
                    return new System.Drawing.Icon(streamInfo.Stream);
                }
                else
                {
                    Debug.WriteLine($"[Bandeja] ✗ GetResourceStream retornou null para {nomeArquivo}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Bandeja] ✗ Erro ao carregar recurso {nomeArquivo}: {ex.Message}");
                Debug.WriteLine($"[Bandeja] StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private void ConfigurarGerenciamentoBandeja(EditarEstudoViewModel viewModel)
        {
            if (_parentWindow == null)
            {
                Debug.WriteLine("[Bandeja] ERRO: _parentWindow é nula em ConfigurarGerenciamentoBandeja");
                return;
            }

            Debug.WriteLine("[Bandeja] Iniciando ConfigurarGerenciamentoBandeja");
            
            // Monitorar mudanças para atualizar ícone da bandeja
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EditarEstudoViewModel.IsTimerAtivo))
                {
                    Debug.WriteLine($"[Bandeja] ✓ IsTimerAtivo alterado para: {viewModel.IsTimerAtivo}");
                    
                    // Apenas atualizar o ícone (Play ou Stop)
                    AtualizarIconoBandeja(viewModel);
                    
                    // Atualizar texto do tooltip
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Text = viewModel.IsTimerAtivo 
                            ? "StudyMinder - Timer (em execução) - Clique para pausar" 
                            : "StudyMinder - Timer (pausado) - Clique para iniciar";
                    }
                }
            };
            
            Debug.WriteLine("[Bandeja] Gerenciamento de bandeja configurado com sucesso");
        }

        private void AtualizarIconoBandeja(EditarEstudoViewModel viewModel)
        {
            if (_notifyIcon == null) return;

            try
            {
                // Apenas trocar a referência do ícone em cache
                if (viewModel.IsTimerAtivo && _stopIcon != null)
                {
                    _notifyIcon.Icon = _stopIcon;
                    Debug.WriteLine("[Bandeja] ✓ Ícone alterado para: Stop.ico");
                }
                else if (!viewModel.IsTimerAtivo && _playIcon != null)
                {
                    _notifyIcon.Icon = _playIcon;
                    Debug.WriteLine("[Bandeja] ✓ Ícone alterado para: Play.ico");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Bandeja] ✗ Erro ao atualizar ícone: {ex.Message}");
            }
        }

        private void MinimizarParaBandeja()
        {
            Debug.WriteLine("[Bandeja] MinimizarParaBandeja() chamado");
            
            if (_parentWindow == null)
            {
                Debug.WriteLine("[Bandeja] ✗ ERRO: _parentWindow é nula");
                return;
            }
            
            if (_notifyIcon == null)
            {
                Debug.WriteLine("[Bandeja] ✗ ERRO: _notifyIcon é nula");
                return;
            }

            try
            {
                _parentWindow.WindowState = WindowState.Minimized;
                //_parentWindow.Visibility = Visibility.Collapsed;
                _notifyIcon.Visible = true;
                _isMinimized = true;
                Debug.WriteLine("[Bandeja] ✓ Janela minimizada para bandeja com sucesso");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Bandeja] ✗ Erro ao minimizar: {ex.Message}");
            }
        }

        private void RestaurarDaBandeja()
        {
            Debug.WriteLine("[Bandeja] RestaurarDaBandeja() chamado");
            
            if (_parentWindow == null)
            {
                Debug.WriteLine("[Bandeja] ✗ ERRO: _parentWindow é nula");
                return;
            }
            
            if (!_isMinimized)
            {
                Debug.WriteLine("[Bandeja] Janela não está minimizada, ignorando");
                return;
            }

            try
            {
                //_parentWindow.Visibility = Visibility.Visible;
                _parentWindow.WindowState = WindowState.Normal;
                _parentWindow.Activate();
                
                // O ícone PERMANECE VISÍVEL - deve existir enquanto a view está carregada
                // e só desaparecer quando a view é fechada (no Unloaded)
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = true;
                }
                
                _isMinimized = false;
                Debug.WriteLine("[Bandeja] ✓ Janela restaurada da bandeja com sucesso");
                Debug.WriteLine("[Bandeja] ✓ Ícone permanece visível na bandeja");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Bandeja] ✗ Erro ao restaurar: {ex.Message}");
            }
        }

        private void DuracaoTextBox_PreviewTextInput(object sender, WInput.TextCompositionEventArgs e)
        {
            // Permitir apenas números
            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        private void DuracaoTextBox_PreviewKeyDown(object sender, WInput.KeyEventArgs e)
        {
            // Permitir teclas de navegação e edição
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || 
                e.Key == Key.Right || e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Home || e.Key == Key.End)
            {
                return;
            }

            // Permitir Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            if (Keyboard.Modifiers == ModifierKeys.Control &&
                (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V || e.Key == Key.X))
            {
                return;
            }
        }

        private void DuracaoTextBox_TextChanged(object sender, WControls.TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;

            var textBox = sender as WControls.TextBox;
            if (textBox == null) return;

            _isUpdatingText = true;

            try
            {
                string text = textBox.Text;
                int caretPosition = textBox.CaretIndex;
                
                // Extrair apenas números e contar posição
                string numbersOnly = Regex.Replace(text, @"[^\d]", "");
                int numbersCount = numbersOnly.Length;
                
                // Aplicar máscara simples
                string masked = ApplySimpleMask(text);
                
                if (text != masked)
                {
                    textBox.Text = masked;
                    
                    // Calcular nova posição do cursor baseada na quantidade total de números
                    int newCaretPosition = CalculateCaretPosition(masked, numbersCount);
                    textBox.CaretIndex = Math.Min(newCaretPosition, masked.Length);
                }
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private void DuracaoTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as WControls.TextBox;
            if (textBox != null)
            {
                // Selecionar todo o texto para fácil substituição
                textBox.SelectAll();
            }
        }

        private void DuracaoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as WControls.TextBox;
            if (textBox == null) return;

            // Validar formato final
            if (!IsValidDurationFormat(textBox.Text))
            {
                textBox.Text = "00:00:00";
                textBox.BorderBrush = new SolidColorBrush(Colors.Red);
            }
            else
            {
                textBox.ClearValue(WControls.TextBox.BorderBrushProperty);
            }
        }

        private string ApplySimpleMask(string input)
        {
            // Remover tudo que não é número
            string numbers = Regex.Replace(input, @"[^\d]", "");
            
            // Limitar a 6 dígitos
            if (numbers.Length > 6)
                numbers = numbers.Substring(0, 6);
            
            // Aplicar máscara baseada no comprimento
            switch (numbers.Length)
            {
                case 0:
                    return "";
                case 1:
                    return numbers;
                case 2:
                    return numbers;
                case 3:
                    return $"{numbers.Substring(0, 2)}:{numbers.Substring(2)}";
                case 4:
                    return $"{numbers.Substring(0, 2)}:{numbers.Substring(2)}";
                case 5:
                    return $"{numbers.Substring(0, 2)}:{numbers.Substring(2, 2)}:{numbers.Substring(4)}";
                case 6:
                    return $"{numbers.Substring(0, 2)}:{numbers.Substring(2, 2)}:{numbers.Substring(4, 2)}";
                default:
                    return numbers;
            }
        }

        private int CalculateCaretPosition(string maskedText, int numbersTyped)
        {
            // Calcular posição do cursor baseada na quantidade de números digitados
            // Sempre posiciona no final do texto formatado
            return maskedText.Length;
        }

        private bool IsValidDurationFormat(string duration)
        {
            if (string.IsNullOrEmpty(duration))
                return false;

            // Verificar formato hh:mm:ss
            var regex = new Regex(@"^([0-1]?[0-9]|2[0-3]):([0-5]?[0-9]):([0-5]?[0-9])$");
            if (!regex.IsMatch(duration))
                return false;

            // Verificar se é um tempo válido
            var parts = duration.Split(':');
            if (parts.Length != 3)
                return false;

            int hours = int.Parse(parts[0]);
            int minutes = int.Parse(parts[1]);
            int seconds = int.Parse(parts[2]);

            return hours <= 23 && minutes <= 59 && seconds <= 59;
        }


    }
}
