using System.ComponentModel;

namespace StudyMinder.Services
{
    /// <summary>
    /// Configurações do sistema de notificações
    /// </summary>
    public class NotificationSettings : INotifyPropertyChanged
    {
        private bool _enableToasts = true;
        private bool _enableSound = false;
        private int _defaultDuration = 5000;
        private bool _enableAnimations = true;
        private int _maxToasts = 5;

        public bool EnableToasts
        {
            get => _enableToasts;
            set
            {
                if (_enableToasts != value)
                {
                    _enableToasts = value;
                    OnPropertyChanged(nameof(EnableToasts));
                }
            }
        }

        public bool EnableSound
        {
            get => _enableSound;
            set
            {
                if (_enableSound != value)
                {
                    _enableSound = value;
                    OnPropertyChanged(nameof(EnableSound));
                }
            }
        }

        public int DefaultDuration
        {
            get => _defaultDuration;
            set
            {
                if (_defaultDuration != value && value > 0)
                {
                    _defaultDuration = value;
                    OnPropertyChanged(nameof(DefaultDuration));
                }
            }
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                if (_enableAnimations != value)
                {
                    _enableAnimations = value;
                    OnPropertyChanged(nameof(EnableAnimations));
                }
            }
        }

        public int MaxToasts
        {
            get => _maxToasts;
            set
            {
                if (_maxToasts != value && value > 0)
                {
                    _maxToasts = value;
                    OnPropertyChanged(nameof(MaxToasts));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
