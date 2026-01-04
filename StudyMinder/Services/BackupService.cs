using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;

namespace StudyMinder.Services
{
    public class BackupService : IBackupService
    {
        private readonly StudyMinderContext _context;
        private readonly IConfigurationService _configurationService;
        private readonly string _databasePath;
        private readonly string _configPath;

        public bool AutoBackupEnabled
        {
            get => _configurationService.Settings.Database.AutoBackupEnabled;
            set => _configurationService.Settings.Database.AutoBackupEnabled = value;
        }

        public int BackupFrequencyDays
        {
            get => _configurationService.Settings.Database.BackupFrequencyDays;
            set => _configurationService.Settings.Database.BackupFrequencyDays = value;
        }

        public int MaxBackupsToKeep
        {
            get => _configurationService.Settings.Database.MaxBackupsToKeep;
            set => _configurationService.Settings.Database.MaxBackupsToKeep = value;
        }

        public string BackupDirectory { get; }

        public event EventHandler<BackupEventArgs>? BackupCreated;
        public event EventHandler<BackupEventArgs>? BackupError;

        public BackupService(StudyMinderContext context, IConfigurationService configurationService)
        {
            _context = context;
            _configurationService = configurationService;

            var appDirectory = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            
            BackupDirectory = Path.Combine(appDirectory, "Backups");
            _databasePath = Path.Combine(appDirectory, "StudyMinder.db");
            _configPath = Path.Combine(appDirectory, "Config", "userprefs.json");

            Directory.CreateDirectory(BackupDirectory);
        }

        public async Task<string> CreateBackupAsync(string? customName = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = customName ?? $"backup_{timestamp}";
            var zipFilePath = Path.Combine(BackupDirectory, $"{backupName}.zip");
            var tempDbPath = Path.ChangeExtension(zipFilePath, ".tmp.db");

            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Não foi possível obter a connection string do banco de dados.");
                }

                using (var source = new SqliteConnection(connectionString))
                using (var destination = new SqliteConnection($"Data Source={tempDbPath};Pooling=False"))
                {
                    await source.OpenAsync();
                    await destination.OpenAsync();
                    source.BackupDatabase(destination);
                }

                await Task.Run(() =>
                {
                    using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
                    archive.CreateEntryFromFile(tempDbPath, "StudyMinder.db");
                    if (File.Exists(_configPath))
                    {
                        archive.CreateEntryFromFile(_configPath, "userprefs.json");
                    }
                });

                BackupCreated?.Invoke(this, new BackupEventArgs(true, zipFilePath));
                return zipFilePath;
            }
            catch (Exception ex)
            {
                BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: ex.Message));
                throw;
            }
            finally
            {
                if (File.Exists(tempDbPath))
                {
                    File.Delete(tempDbPath);
                }
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            var tempDbPath = Path.Combine(Path.GetDirectoryName(backupFilePath)!, "restore.tmp.db");

            try
            {
                if (!File.Exists(backupFilePath))
                    return false;

                using (var archive = ZipFile.OpenRead(backupFilePath))
                {
                    var dbEntry = archive.GetEntry("StudyMinder.db");
                    if (dbEntry != null)
                    {
                        dbEntry.ExtractToFile(tempDbPath, overwrite: true);
                    }

                    var configEntry = archive.GetEntry("userprefs.json");
                    if (configEntry != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
                        configEntry.ExtractToFile(_configPath, overwrite: true);
                    }
                }

                if (File.Exists(tempDbPath))
                {
                    var mainDbConnectionString = _context.Database.GetDbConnection().ConnectionString;
                    
                    // Explicitamente fechar a conexão principal para liberar o arquivo .db
                    await _context.Database.CloseConnectionAsync();

                    // A API de backup do SQLite fará a cópia do banco de dados temporário sobre o principal
                    using (var source = new SqliteConnection($"Data Source={tempDbPath};Pooling=False"))
                    using (var destination = new SqliteConnection(mainDbConnectionString))
                    {
                        await source.OpenAsync();
                        await destination.OpenAsync();
                        source.BackupDatabase(destination);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: ex.Message));
                return false;
            }
            finally
            {
                if (File.Exists(tempDbPath))
                {
                    File.Delete(tempDbPath);
                }
            }
        }

        public async Task<List<string>> GetAvailableBackupsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return Directory.GetFiles(BackupDirectory, "*.zip")
                        .Select(Path.GetFileName)
                        .Where(name => name != null)
                        .ToList()!;
                }
                catch (Exception ex)
                {
                    BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: ex.Message));
                    return new List<string>();
                }
            });
        }

        public async Task<bool> DeleteBackupAsync(string backupFileName)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var backupPath = Path.Combine(BackupDirectory, backupFileName);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                        return true;
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: ex.Message));
                return false;
            }
        }

        public async Task CleanupOldBackupsAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    var backups = (await GetAvailableBackupsAsync())
                        .Select(name => new FileInfo(Path.Combine(BackupDirectory, name!)))
                        .OrderByDescending(fi => fi.CreationTime)
                        .ToList();

                    if (backups.Count > _configurationService.Settings.Database.MaxBackupsToKeep)
                    {
                        var toDelete = backups.Skip(_configurationService.Settings.Database.MaxBackupsToKeep);
                        foreach (var backup in toDelete)
                        {
                            backup.Delete();
                        }
                    }
                });
            }
            catch(Exception ex)
            {
                BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: ex.Message));
            }
        }

        public async Task CheckAndRunAutoBackupAsync()
        {
            try
            {
                if (!_configurationService.Settings.Database.AutoBackupEnabled)
                    return;

                var backups = await GetAvailableBackupsAsync();
                
                if (backups.Count == 0)
                {
                    // Nenhum backup anterior, criar um agora
                    await CreateBackupAsync($"auto_backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                    await CleanupOldBackupsAsync();
                    return;
                }

                // Encontrar o backup mais recente
                var latestBackupName = backups
                    .Select(name => new FileInfo(Path.Combine(BackupDirectory, name)))
                    .OrderByDescending(fi => fi.CreationTime)
                    .FirstOrDefault();

                if (latestBackupName == null)
                    return;

                var daysSinceLastBackup = (DateTime.Now - latestBackupName.CreationTime).TotalDays;

                if (daysSinceLastBackup >= _configurationService.Settings.Database.BackupFrequencyDays)
                {
                    // Criar novo backup silenciosamente
                    await CreateBackupAsync($"auto_backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                    await CleanupOldBackupsAsync();
                }
            }
            catch (Exception ex)
            {
                BackupError?.Invoke(this, new BackupEventArgs(false, errorMessage: $"Erro ao verificar auto backup: {ex.Message}"));
            }
        }
    }
}
