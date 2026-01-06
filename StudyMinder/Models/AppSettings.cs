using System.ComponentModel;

namespace StudyMinder.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Aparência
        public AppearanceSettings Appearance { get; set; } = new();

        // Notificações
        public NotificationSettings Notifications { get; set; } = new();

        // Metas
        public GoalSettings Goals { get; set; } = new();

        // Estudo
        public StudySettings Study { get; set; } = new();

        // Banco de Dados
        public DatabaseSettings Database { get; set; } = new();

        // Arquivamento
        public ArchivingSettings Archiving { get; set; } = new();

        // Janela
        public WindowSettings Window { get; set; } = new();
    }

    public class WindowSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _width = 1280;
        public int Width
        {
            get => _width;
            set { if (_width != value) { _width = value; OnPropertyChanged(nameof(Width)); } }
        }

        private int _height = 800;
        public int Height
        {
            get => _height;
            set { if (_height != value) { _height = value; OnPropertyChanged(nameof(Height)); } }
        }

        private int _left = 100;
        public int Left
        {
            get => _left;
            set { if (_left != value) { _left = value; OnPropertyChanged(nameof(Left)); } }
        }

        private int _top = 100;
        public int Top
        {
            get => _top;
            set { if (_top != value) { _top = value; OnPropertyChanged(nameof(Top)); } }
        }

        private bool _maximized = false;
        public bool Maximized
        {
            get => _maximized;
            set { if (_maximized != value) { _maximized = value; OnPropertyChanged(nameof(Maximized)); } }
        }
    }


    public class AppearanceSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _theme = "System";
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged(nameof(Theme));
                }
            }
        }

        private string _layoutMode = "List";
        public string LayoutMode
        {
            get => _layoutMode;
            set
            {
                if (_layoutMode != value)
                {
                    _layoutMode = value;
                    OnPropertyChanged(nameof(LayoutMode));
                }
            }
        }
    }

    public class NotificationSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _editalRemindersEnabled = true;
        public bool EditalRemindersEnabled
        {
            get => _editalRemindersEnabled;
            set
            {
                if (_editalRemindersEnabled != value)
                {
                    _editalRemindersEnabled = value;
                    OnPropertyChanged(nameof(EditalRemindersEnabled));
                }
            }
        }

        private bool _goalNotificationsEnabled = true;
        public bool GoalNotificationsEnabled
        {
            get => _goalNotificationsEnabled;
            set
            {
                if (_goalNotificationsEnabled != value)
                {
                    _goalNotificationsEnabled = value;
                    OnPropertyChanged(nameof(GoalNotificationsEnabled));
                }
            }
        }
    }

    public class GoalSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _weeklyStudyHoursGoal = 20;
        public int WeeklyStudyHoursGoal
        {
            get => _weeklyStudyHoursGoal;
            set
            {
                if (_weeklyStudyHoursGoal != value)
                {
                    _weeklyStudyHoursGoal = value;
                    OnPropertyChanged(nameof(WeeklyStudyHoursGoal));
                }
            }
        }

        private int _weeklyQuestionsGoal = 100;
        public int WeeklyQuestionsGoal
        {
            get => _weeklyQuestionsGoal;
            set
            {
                if (_weeklyQuestionsGoal != value)
                {
                    _weeklyQuestionsGoal = value;
                    OnPropertyChanged(nameof(WeeklyQuestionsGoal));
                }
            }
        }

        private int _weeklyPagesGoal = 50;
        public int WeeklyPagesGoal
        {
            get => _weeklyPagesGoal;
            set
            {
                if (_weeklyPagesGoal != value)
                {
                    _weeklyPagesGoal = value;
                    OnPropertyChanged(nameof(WeeklyPagesGoal));
                }
            }
        }
    }

    public class StudySettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _pomodoroEnabled = false;
        public bool PomodoroEnabled
        {
            get => _pomodoroEnabled;
            set
            {
                if (_pomodoroEnabled != value)
                {
                    _pomodoroEnabled = value;
                    OnPropertyChanged(nameof(PomodoroEnabled));
                }
            }
        }

        private int _pomodoroFocusMinutes = 25;
        public int PomodoroFocusMinutes
        {
            get => _pomodoroFocusMinutes;
            set
            {
                if (_pomodoroFocusMinutes != value)
                {
                    _pomodoroFocusMinutes = value;
                    OnPropertyChanged(nameof(PomodoroFocusMinutes));
                }
            }
        }

        private int _pomodoroBreakMinutes = 5;
        public int PomodoroBreakMinutes
        {
            get => _pomodoroBreakMinutes;
            set
            {
                if (_pomodoroBreakMinutes != value)
                {
                    _pomodoroBreakMinutes = value;
                    OnPropertyChanged(nameof(PomodoroBreakMinutes));
                }
            }
        }

        private bool _minimizeToTrayOnStart = false;
        public bool MinimizeToTrayOnStart
        {
            get => _minimizeToTrayOnStart;
            set
            {
                if (_minimizeToTrayOnStart != value)
                {
                    _minimizeToTrayOnStart = value;
                    OnPropertyChanged(nameof(MinimizeToTrayOnStart));
                }
            }
        }

        private bool _maximizeTimerOnStudy = false;
        public bool MaximizeTimerOnStudy
        {
            get => _maximizeTimerOnStudy;
            set
            {
                if (_maximizeTimerOnStudy != value)
                {
                    _maximizeTimerOnStudy = value;
                    OnPropertyChanged(nameof(MaximizeTimerOnStudy));
                }
            }
        }

        // Configurações para Revisão 4.2
        private bool _method42MondayEnabled = false;
        public bool Method42MondayEnabled
        {
            get => _method42MondayEnabled;
            set
            {
                if (_method42MondayEnabled != value)
                {
                    _method42MondayEnabled = value;
                    OnPropertyChanged(nameof(Method42MondayEnabled));
                }
            }
        }

        private bool _method42TuesdayEnabled = false;
        public bool Method42TuesdayEnabled
        {
            get => _method42TuesdayEnabled;
            set
            {
                if (_method42TuesdayEnabled != value)
                {
                    _method42TuesdayEnabled = value;
                    OnPropertyChanged(nameof(Method42TuesdayEnabled));
                }
            }
        }

        private bool _method42WednesdayEnabled = false;
        public bool Method42WednesdayEnabled
        {
            get => _method42WednesdayEnabled;
            set
            {
                if (_method42WednesdayEnabled != value)
                {
                    _method42WednesdayEnabled = value;
                    OnPropertyChanged(nameof(Method42WednesdayEnabled));
                }
            }
        }

        private bool _method42ThursdayEnabled = false;
        public bool Method42ThursdayEnabled
        {
            get => _method42ThursdayEnabled;
            set
            {
                if (_method42ThursdayEnabled != value)
                {
                    _method42ThursdayEnabled = value;
                    OnPropertyChanged(nameof(Method42ThursdayEnabled));
                }
            }
        }

        private bool _method42FridayEnabled = false;
        public bool Method42FridayEnabled
        {
            get => _method42FridayEnabled;
            set
            {
                if (_method42FridayEnabled != value)
                {
                    _method42FridayEnabled = value;
                    OnPropertyChanged(nameof(Method42FridayEnabled));
                }
            }
        }

        private bool _method42SaturdayEnabled = true;
        public bool Method42SaturdayEnabled
        {
            get => _method42SaturdayEnabled;
            set
            {
                if (_method42SaturdayEnabled != value)
                {
                    _method42SaturdayEnabled = value;
                    OnPropertyChanged(nameof(Method42SaturdayEnabled));
                }
            }
        }

        private bool _method42SundayEnabled = true;
        public bool Method42SundayEnabled
        {
            get => _method42SundayEnabled;
            set
            {
                if (_method42SundayEnabled != value)
                {
                    _method42SundayEnabled = value;
                    OnPropertyChanged(nameof(Method42SundayEnabled));
                }
            }
        }
    }

    public class DatabaseSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _autoBackupEnabled = true;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set
            {
                if (_autoBackupEnabled != value)
                {
                    _autoBackupEnabled = value;
                    OnPropertyChanged(nameof(AutoBackupEnabled));
                }
            }
        }

        private int _backupFrequencyDays = 7;
        public int BackupFrequencyDays
        {
            get => _backupFrequencyDays;
            set
            {
                if (_backupFrequencyDays != value)
                {
                    _backupFrequencyDays = value;
                    OnPropertyChanged(nameof(BackupFrequencyDays));
                }
            }
        }

        private int _maxBackupsToKeep = 10;
        public int MaxBackupsToKeep
        {
            get => _maxBackupsToKeep;
            set
            {
                if (_maxBackupsToKeep != value)
                {
                    _maxBackupsToKeep = value;
                    OnPropertyChanged(nameof(MaxBackupsToKeep));
                }
            }
        }
    }

    public class ArchivingSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _hideArchivedDisciplines = true;
        public bool HideArchivedDisciplines
        {
            get => _hideArchivedDisciplines;
            set
            {
                if (_hideArchivedDisciplines != value)
                {
                    _hideArchivedDisciplines = value;
                    OnPropertyChanged(nameof(HideArchivedDisciplines));
                }
            }
        }

        private bool _hideInactiveEditals = true;
        public bool HideInactiveEditals
        {
            get => _hideInactiveEditals;
            set
            {
                if (_hideInactiveEditals != value)
                {
                    _hideInactiveEditals = value;
                    OnPropertyChanged(nameof(HideInactiveEditals));
                }
            }
        }
    }
}
