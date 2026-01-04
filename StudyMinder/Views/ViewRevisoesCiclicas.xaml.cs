using System.Windows.Controls;
using System.Windows.Input;

namespace StudyMinder.Views
{
    public partial class ViewRevisoesCiclicas : UserControl
    {
        public ViewRevisoesCiclicas()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ViewRevisoesCiclicas.Constructor - Inicializando...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[DEBUG] ViewRevisoesCiclicas.Constructor - Inicializado com sucesso");
        }

        private void ViewRevisoesCiclicas_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ViewRevisoesCiclicas.Loaded - View carregada");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DataContext Type: {DataContext?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DataContext: {DataContext}");
            
            if (DataContext is ViewModels.RevisoesCiclicasViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ✅ ViewModel encontrado");
                System.Diagnostics.Debug.WriteLine("[DEBUG] Comandos disponíveis no ViewModel");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ❌ ERRO: DataContext não é RevisoesCiclicasViewModel");
            }
        }

        private void DebugButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] DebugButton_Click chamado - Botão responde ao click!");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DataContext: {DataContext?.GetType().Name}");
            
            // Tentar executar o comando manualmente
            if (DataContext is ViewModels.RevisoesCiclicasViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ViewModel encontrado, tentando executar comando...");
                // Usar o método público gerado pelo CommunityToolkit
                vm.ToggleEditAssuntosCommandCommand.Execute(null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ERRO: DataContext não é RevisoesCiclicasViewModel");
            }
        }

        // Adicionar handler para rastrear cliques nos botões
        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Button_PreviewMouseDown - Botão clicado");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Button.Command: {btn.Command}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Button.CommandParameter: {btn.CommandParameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Button.IsEnabled: {btn.IsEnabled}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Button.ToolTip: {btn.ToolTip}");
                
                if (btn.Command != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Comando vinculado: {btn.Command.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] CanExecute: {btn.Command.CanExecute(btn.CommandParameter)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] ❌ ERRO: Nenhum comando vinculado ao botão!");
                }
            }
        }
    }
}
