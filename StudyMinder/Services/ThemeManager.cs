using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace StudyMinder.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public interface IThemeManager
    {
        AppTheme CurrentTheme { get; }
        void SetTheme(AppTheme theme);
        void SetTheme(string themeName);
        bool TestThemeLoading();
        event EventHandler<AppTheme>? ThemeChanged;
    }

    public class ThemeManager : IThemeManager
    {
        private AppTheme _currentTheme = AppTheme.System;
        private bool _isSystemThemeListenerActive = false;

        public AppTheme CurrentTheme => _currentTheme;

        public event EventHandler<AppTheme>? ThemeChanged;

        public ThemeManager()
        {
            // Não aplicar tema no construtor para evitar conflito com App.xaml
            // O tema inicial já está definido no App.xaml
        }

        public void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme) return;

            _currentTheme = theme;

            switch (theme)
            {
                case AppTheme.Light:
                    ApplyLightTheme();
                    StopSystemThemeListener();
                    break;
                case AppTheme.Dark:
                    ApplyDarkTheme();
                    StopSystemThemeListener();
                    break;
                case AppTheme.System:
                    ApplySystemTheme();
                    StartSystemThemeListener();
                    break;
            }

            ThemeChanged?.Invoke(this, theme);
        }

        public void SetTheme(string themeName)
        {
            if (Enum.TryParse<AppTheme>(themeName, true, out var theme))
            {
                SetTheme(theme);
            }
        }

        private void ApplyLightTheme()
        {
            try
            {
                // Remover apenas temas existentes, preservar outros recursos
                RemoveExistingThemes();
                
                // Adicionar recursos do tema claro
                var lightTheme = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute)
                };
                
                Application.Current.Resources.MergedDictionaries.Add(lightTheme);
                System.Diagnostics.Debug.WriteLine("[ThemeManager] Tema claro aplicado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Erro ao aplicar tema claro: {ex.Message}");
                throw;
            }
        }

        private void ApplyDarkTheme()
        {
            try
            {
                // Remover apenas temas existentes, preservar outros recursos
                RemoveExistingThemes();
                
                // Adicionar recursos do tema escuro
                var darkTheme = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Resources/Themes/Dark.xaml", UriKind.Absolute)
                };
                
                Application.Current.Resources.MergedDictionaries.Add(darkTheme);
                System.Diagnostics.Debug.WriteLine("[ThemeManager] Tema escuro aplicado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Erro ao aplicar tema escuro: {ex.Message}");
                throw;
            }
        }

        private void RemoveExistingThemes()
        {
            // Remover apenas os ResourceDictionaries de tema, preservando outros recursos
            var themesToRemove = Application.Current.Resources.MergedDictionaries
                .Where(rd => rd.Source != null && 
                           (rd.Source.ToString().Contains("/Themes/Light.xaml") || 
                            rd.Source.ToString().Contains("/Themes/Dark.xaml")))
                .ToList();

            foreach (var theme in themesToRemove)
            {
                Application.Current.Resources.MergedDictionaries.Remove(theme);
            }
        }

        private void ApplySystemTheme()
        {
            var isSystemDark = IsSystemDarkTheme();
            
            if (isSystemDark)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
        }

        private void StartSystemThemeListener()
        {
            if (_isSystemThemeListenerActive) return;

            _isSystemThemeListenerActive = true;
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
        }

        private void StopSystemThemeListener()
        {
            if (!_isSystemThemeListenerActive) return;

            _isSystemThemeListenerActive = false;
            SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
        }

        private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && _currentTheme == AppTheme.System)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplySystemTheme();
                    ThemeChanged?.Invoke(this, AppTheme.System);
                });
            }
        }

        private static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = Dark theme, 1 = Light theme
                }
            }
            catch
            {
                // Em caso de erro, assumir tema claro
            }

            return false;
        }

        public bool TestThemeLoading()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ThemeManager] Iniciando teste de carregamento de temas...");
                
                // Testar carregamento do tema claro
                var lightUri = new Uri("pack://application:,,,/Resources/Themes/Light.xaml", UriKind.Absolute);
                var lightTheme = new ResourceDictionary { Source = lightUri };
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Tema claro carregado: {lightTheme.Count} recursos");
                
                // Testar carregamento do tema escuro
                var darkUri = new Uri("pack://application:,,,/Resources/Themes/Dark.xaml", UriKind.Absolute);
                var darkTheme = new ResourceDictionary { Source = darkUri };
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Tema escuro carregado: {darkTheme.Count} recursos");
                
                // Verificar se recursos específicos existem
                var hasBackgroundBrush = lightTheme.Contains("BackgroundBrush");
                var hasPrimaryBrush = lightTheme.Contains("PrimaryBrush");
                
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Recursos encontrados - BackgroundBrush: {hasBackgroundBrush}, PrimaryBrush: {hasPrimaryBrush}");
                
                return lightTheme.Count > 0 && darkTheme.Count > 0 && hasBackgroundBrush && hasPrimaryBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Erro no teste de carregamento: {ex.Message}");
                return false;
            }
        }

        ~ThemeManager()
        {
            StopSystemThemeListener();
        }
    }
}
