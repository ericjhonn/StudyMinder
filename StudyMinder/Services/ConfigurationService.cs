using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using StudyMinder.Models;

namespace StudyMinder.Services
{
    public interface IConfigurationService
    {
        AppSettings Settings { get; }
        Task LoadAsync();
        Task SaveAsync();
        Task ResetToDefaultsAsync();
        Task<AppSettings> LoadSettingsAsync();
        event EventHandler<AppSettings>? SettingsChanged;
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public event EventHandler<AppSettings>? SettingsChanged;

        public ConfigurationService()
        {
            try
            {
                // Obter o diretório do aplicativo (onde o executável está)
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var configDirectory = Path.Combine(appDirectory, "Config");
                
                // Garantir que o diretório existe e tem permissões de escrita
                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }
                else
                {
                    // Verificar permissão de escrita
                    var testFile = Path.Combine(configDirectory, "write_test.tmp");
                    try
                    {
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Se não tiver permissão, usar a pasta do usuário
                        var appData = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "StudyMinder");
                        
                        if (!Directory.Exists(appData))
                            Directory.CreateDirectory(appData);
                            
                        configDirectory = Path.Combine(appData, "Config");
                        if (!Directory.Exists(configDirectory))
                            Directory.CreateDirectory(configDirectory);
                    }
                }

                _configFilePath = Path.Combine(configDirectory, "appsettings.json");
                _settings = new AppSettings();

                // Subscrever aos eventos de mudança das configurações
                SubscribeToSettingsChanges();
            }
            catch (Exception ex)
            {
                // Se tudo falhar, usar o diretório temporário como último recurso
                var tempDir = Path.Combine(Path.GetTempPath(), "StudyMinder_Config");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);
                    
                _configFilePath = Path.Combine(tempDir, "appsettings.json");
                _settings = new AppSettings();
                
                System.Diagnostics.Debug.WriteLine($"[Config] Erro ao configurar diretório de configurações: {ex.Message}. Usando diretório temporário: {_configFilePath}");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    // Tentar ler o arquivo principal
                    try
                    {
                        var json = await File.ReadAllTextAsync(_configFilePath);
                        var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, GetJsonOptions());
                        
                        if (loadedSettings != null)
                        {
                            _settings = loadedSettings;
                            SubscribeToSettingsChanges();
                            SettingsChanged?.Invoke(this, _settings);
                            return;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Config] Erro ao analisar arquivo de configuração: {jsonEx.Message}");
                        // Tentar carregar o backup se disponível
                        var backupFile = _configFilePath + ".bak";
                        if (File.Exists(backupFile))
                        {
                            try
                            {
                                var backupJson = await File.ReadAllTextAsync(backupFile);
                                var backupSettings = JsonSerializer.Deserialize<AppSettings>(backupJson, GetJsonOptions());
                                if (backupSettings != null)
                                {
                                    _settings = backupSettings;
                                    // Tentar reparar o arquivo principal
                                    await SaveAsync();
                                    SubscribeToSettingsChanges();
                                    SettingsChanged?.Invoke(this, _settings);
                                    return;
                                }
                            }
                            catch (Exception backupEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Config] Falha ao carregar backup: {backupEx.Message}");
                            }
                        }
                    }
                }
                
                // Se chegou aqui, não conseguiu carregar as configurações
                System.Diagnostics.Debug.WriteLine("[Config] Usando configurações padrão");
                _settings = new AppSettings();
                await SaveAsync(); // Criar arquivo com configurações padrão
                SubscribeToSettingsChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] Erro ao carregar configurações: {ex.Message}");
                _settings = new AppSettings();
                SubscribeToSettingsChanges();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                // Criar uma cópia temporária para escrita atômica
                var tempFile = _configFilePath + ".tmp";
                var json = JsonSerializer.Serialize(_settings, GetJsonOptions());
                
                // Escrever para um arquivo temporário primeiro
                await File.WriteAllTextAsync(tempFile, json);
                
                // Se o arquivo de destino existir, fazer backup
                if (File.Exists(_configFilePath))
                {
                    var backupFile = _configFilePath + ".bak";
                    File.Copy(_configFilePath, backupFile, true);
                }
                
                // Substituir o arquivo existente pelo novo
                File.Move(tempFile, _configFilePath, true);
                
                // Notificar sobre a mudança
                SettingsChanged?.Invoke(this, _settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] Erro ao salvar configurações em {_configFilePath}: {ex.Message}");
                
                // Tentar salvar em um local alternativo se possível
                try
                {
                    var altPath = Path.Combine(Path.GetTempPath(), "StudyMinder_Config", "appsettings.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(altPath)!);
                    await File.WriteAllTextAsync(altPath, JsonSerializer.Serialize(_settings, GetJsonOptions()));
                    System.Diagnostics.Debug.WriteLine($"[Config] Configurações salvas em local alternativo: {altPath}");
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Config] Falha ao salvar configurações em local alternativo: {innerEx.Message}");
                    throw new Exception("Não foi possível salvar as configurações em nenhum local disponível.", ex);
                }
            }
        }

        public async Task ResetToDefaultsAsync()
        {
            _settings = new AppSettings();
            SubscribeToSettingsChanges();
            await SaveAsync();
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            await LoadAsync();
            return _settings;
        }

        private void SubscribeToSettingsChanges()
        {
            if (_settings?.Appearance != null)
            {
                _settings.Appearance.PropertyChanged += (s, e) => _ = SaveAsync();
            }
            
            if (_settings?.Notifications != null)
            {
                _settings.Notifications.PropertyChanged += (s, e) => _ = SaveAsync();
            }
            
            if (_settings?.Goals != null)
            {
                _settings.Goals.PropertyChanged += (s, e) => _ = SaveAsync();
            }
            
            if (_settings?.Study != null)
            {
                _settings.Study.PropertyChanged += (s, e) => _ = SaveAsync();
            }
            
            if (_settings?.Database != null)
            {
                _settings.Database.PropertyChanged += (s, e) => _ = SaveAsync();
            }
            
            if (_settings?.Archiving != null)
            {
                _settings.Archiving.PropertyChanged += (s, e) => _ = SaveAsync();
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }
    }
}
