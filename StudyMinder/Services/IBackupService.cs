using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class BackupEventArgs : EventArgs
    {
        public bool Success { get; }
        public string? FilePath { get; }
        public string? ErrorMessage { get; }

        public BackupEventArgs(bool success, string? filePath = null, string? errorMessage = null)
        {
            Success = success;
            FilePath = filePath;
            ErrorMessage = errorMessage;
        }
    }

    public interface IBackupService
    {
        bool AutoBackupEnabled { get; set; }
        int BackupFrequencyDays { get; set; }
        int MaxBackupsToKeep { get; set; }
        string BackupDirectory { get; }

        Task<string> CreateBackupAsync(string? customName = null);
        Task<bool> RestoreBackupAsync(string backupFilePath);
        Task<List<string>> GetAvailableBackupsAsync();
        Task<bool> DeleteBackupAsync(string backupFileName);
        Task CleanupOldBackupsAsync();
        Task CheckAndRunAutoBackupAsync();

        event EventHandler<BackupEventArgs>? BackupCreated;
        event EventHandler<BackupEventArgs>? BackupError;
    }
}