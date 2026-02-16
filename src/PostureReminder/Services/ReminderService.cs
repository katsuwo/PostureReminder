using System.Windows.Threading;
using PostureReminder.Models;

namespace PostureReminder.Services;

public enum ReminderState
{
    Stopped,
    Running,
    PausedByIdle,
    PausedByDialog,
    PausedByUser
}

public class ReminderService
{
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly IdleDetectionService _idleDetectionService;

    private readonly DispatcherTimer _mainTimer;
    private readonly DispatcherTimer _idleTimer;

    private DateTime _timerStartedAt;
    private TimeSpan _elapsed;

    public ReminderState State { get; private set; } = ReminderState.Stopped;

    public event Action? StateChanged;

    public TimeSpan Remaining
    {
        get
        {
            var interval = TimeSpan.FromMinutes(_settingsService.CurrentSettings.IntervalMinutes);
            if (State == ReminderState.Running)
            {
                var currentElapsed = _elapsed + (DateTime.UtcNow - _timerStartedAt);
                var remaining = interval - currentElapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            var r = interval - _elapsed;
            return r > TimeSpan.Zero ? r : TimeSpan.Zero;
        }
    }

    public ReminderService(
        SettingsService settingsService,
        NotificationService notificationService,
        IdleDetectionService idleDetectionService)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _idleDetectionService = idleDetectionService;

        _mainTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _mainTimer.Tick += OnMainTimerTick;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += OnIdleTimerTick;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void Start()
    {
        ResetAndRun();
        _idleTimer.Start();
    }

    public void Stop()
    {
        _mainTimer.Stop();
        _idleTimer.Stop();
        State = ReminderState.Stopped;
        StateChanged?.Invoke();
    }

    public void PauseByUser()
    {
        if (State == ReminderState.Running)
        {
            _elapsed += DateTime.UtcNow - _timerStartedAt;
            _mainTimer.Stop();
            State = ReminderState.PausedByUser;
            StateChanged?.Invoke();
        }
    }

    public void ResumeByUser()
    {
        if (State == ReminderState.PausedByUser)
        {
            _timerStartedAt = DateTime.UtcNow;
            _mainTimer.Start();
            State = ReminderState.Running;
            StateChanged?.Invoke();
        }
    }

    private void ResetAndRun()
    {
        _elapsed = TimeSpan.Zero;
        _timerStartedAt = DateTime.UtcNow;
        _mainTimer.Start();
        State = ReminderState.Running;
        StateChanged?.Invoke();
    }

    private void OnMainTimerTick(object? sender, EventArgs e)
    {
        if (State != ReminderState.Running) return;

        var totalElapsed = _elapsed + (DateTime.UtcNow - _timerStartedAt);
        var interval = TimeSpan.FromMinutes(_settingsService.CurrentSettings.IntervalMinutes);

        if (totalElapsed >= interval)
        {
            _mainTimer.Stop();
            State = ReminderState.PausedByDialog;
            StateChanged?.Invoke();

            FireNotifications();

            ResetAndRun();
        }
    }

    private void FireNotifications()
    {
        var settings = _settingsService.CurrentSettings;
        var message = settings.ReminderMessage;

        if (settings.PlaySound)
        {
            _notificationService.PlaySound(settings.CustomSoundPath);
        }

        if (settings.ShowToast)
        {
            _notificationService.ShowToast(message);
        }

        if (settings.ShowDialog)
        {
            _notificationService.ShowReminderDialog(message);
        }
    }

    private void OnIdleTimerTick(object? sender, EventArgs e)
    {
        var idleTime = _idleDetectionService.GetIdleTime();
        var threshold = TimeSpan.FromMinutes(_settingsService.CurrentSettings.IdleThresholdMinutes);

        switch (State)
        {
            case ReminderState.Running when idleTime >= threshold:
                _elapsed += DateTime.UtcNow - _timerStartedAt;
                _mainTimer.Stop();
                State = ReminderState.PausedByIdle;
                StateChanged?.Invoke();
                break;

            case ReminderState.PausedByIdle when idleTime < threshold:
                ResetAndRun();
                break;
        }
    }

    private void OnSettingsChanged()
    {
        if (State == ReminderState.Running || State == ReminderState.PausedByIdle)
        {
            ResetAndRun();
        }
    }
}
