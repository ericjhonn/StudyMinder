using System;
using System.Windows.Threading;

namespace StudyMinder.Services
{
    public enum PomodoroState
    {
        Stopped,
        Focus,
        Break,
        Paused
    }

    public class PomodoroTimerEventArgs : EventArgs
    {
        public PomodoroState State { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int CurrentCycle { get; set; }
    }

    public interface IPomodoroTimerService
    {
        PomodoroState CurrentState { get; }
        TimeSpan TimeRemaining { get; }
        TimeSpan TotalTime { get; }
        int CurrentCycle { get; }
        bool IsEnabled { get; set; }
        int FocusMinutes { get; set; }
        int BreakMinutes { get; set; }

        void Start();
        void Pause();
        void Resume();
        void Stop();
        void Reset();

        event EventHandler<PomodoroTimerEventArgs>? StateChanged;
        event EventHandler<PomodoroTimerEventArgs>? Tick;
        event EventHandler<PomodoroTimerEventArgs>? CycleCompleted;
    }

    public class PomodoroTimerService : IPomodoroTimerService
    {
        private readonly DispatcherTimer _timer;
        private PomodoroState _currentState = PomodoroState.Stopped;
        private TimeSpan _timeRemaining;
        private TimeSpan _totalTime;
        private int _currentCycle = 1;
        private bool _isEnabled = false;
        private int _focusMinutes = 25;
        private int _breakMinutes = 5;

        public PomodoroState CurrentState => _currentState;
        public TimeSpan TimeRemaining => _timeRemaining;
        public TimeSpan TotalTime => _totalTime;
        public int CurrentCycle => _currentCycle;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (!value && _currentState != PomodoroState.Stopped)
                    {
                        Stop();
                    }
                }
            }
        }

        public int FocusMinutes
        {
            get => _focusMinutes;
            set
            {
                if (_focusMinutes != value && value > 0)
                {
                    _focusMinutes = value;
                    if (_currentState == PomodoroState.Focus)
                    {
                        Reset();
                    }
                }
            }
        }

        public int BreakMinutes
        {
            get => _breakMinutes;
            set
            {
                if (_breakMinutes != value && value > 0)
                {
                    _breakMinutes = value;
                    if (_currentState == PomodoroState.Break)
                    {
                        Reset();
                    }
                }
            }
        }

        public event EventHandler<PomodoroTimerEventArgs>? StateChanged;
        public event EventHandler<PomodoroTimerEventArgs>? Tick;
        public event EventHandler<PomodoroTimerEventArgs>? CycleCompleted;

        public PomodoroTimerService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            if (!_isEnabled) return;

            if (_currentState == PomodoroState.Stopped)
            {
                StartFocusSession();
            }
            else if (_currentState == PomodoroState.Paused)
            {
                Resume();
            }
        }

        public void Pause()
        {
            if (_currentState == PomodoroState.Focus || _currentState == PomodoroState.Break)
            {
                _timer.Stop();
                _currentState = PomodoroState.Paused;
                OnStateChanged();
            }
        }

        public void Resume()
        {
            if (_currentState == PomodoroState.Paused)
            {
                _timer.Start();
                _currentState = _timeRemaining > TimeSpan.Zero ? 
                    (_totalTime.TotalMinutes == _focusMinutes ? PomodoroState.Focus : PomodoroState.Break) : 
                    PomodoroState.Focus;
                OnStateChanged();
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _currentState = PomodoroState.Stopped;
            _timeRemaining = TimeSpan.Zero;
            _totalTime = TimeSpan.Zero;
            _currentCycle = 1;
            OnStateChanged();
        }

        public void Reset()
        {
            Stop();
            if (_isEnabled)
            {
                StartFocusSession();
            }
        }

        private void StartFocusSession()
        {
            _currentState = PomodoroState.Focus;
            _totalTime = TimeSpan.FromMinutes(_focusMinutes);
            _timeRemaining = _totalTime;
            _timer.Start();
            OnStateChanged();
        }

        private void StartBreakSession()
        {
            _currentState = PomodoroState.Break;
            _totalTime = TimeSpan.FromMinutes(_breakMinutes);
            _timeRemaining = _totalTime;
            _timer.Start();
            OnStateChanged();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));

            OnTick();

            if (_timeRemaining <= TimeSpan.Zero)
            {
                _timer.Stop();
                
                if (_currentState == PomodoroState.Focus)
                {
                    OnCycleCompleted();
                    StartBreakSession();
                }
                else if (_currentState == PomodoroState.Break)
                {
                    _currentCycle++;
                    StartFocusSession();
                }
            }
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, new PomodoroTimerEventArgs
            {
                State = _currentState,
                TimeRemaining = _timeRemaining,
                TotalTime = _totalTime,
                CurrentCycle = _currentCycle
            });
        }

        private void OnTick()
        {
            Tick?.Invoke(this, new PomodoroTimerEventArgs
            {
                State = _currentState,
                TimeRemaining = _timeRemaining,
                TotalTime = _totalTime,
                CurrentCycle = _currentCycle
            });
        }

        private void OnCycleCompleted()
        {
            CycleCompleted?.Invoke(this, new PomodoroTimerEventArgs
            {
                State = _currentState,
                TimeRemaining = _timeRemaining,
                TotalTime = _totalTime,
                CurrentCycle = _currentCycle
            });
        }
    }
}
