using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PostureReminder.Models;
using PostureReminder.Services;

namespace PostureReminder.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly AutoStartService _autoStartService;

    private int _intervalMinutes;
    private int _idleThresholdMinutes;
    private bool _showToast;
    private bool _playSound;
    private bool _showDialog;
    private string? _customSoundPath;
    private bool _autoStart;
    private string _reminderMessage = string.Empty;

    public int IntervalMinutes
    {
        get => _intervalMinutes;
        set => SetField(ref _intervalMinutes, value);
    }

    public int IdleThresholdMinutes
    {
        get => _idleThresholdMinutes;
        set => SetField(ref _idleThresholdMinutes, value);
    }

    public bool ShowToast
    {
        get => _showToast;
        set => SetField(ref _showToast, value);
    }

    public bool PlaySound
    {
        get => _playSound;
        set => SetField(ref _playSound, value);
    }

    public bool ShowDialog
    {
        get => _showDialog;
        set => SetField(ref _showDialog, value);
    }

    public string? CustomSoundPath
    {
        get => _customSoundPath;
        set => SetField(ref _customSoundPath, value);
    }

    public bool AutoStart
    {
        get => _autoStart;
        set => SetField(ref _autoStart, value);
    }

    public string ReminderMessage
    {
        get => _reminderMessage;
        set => SetField(ref _reminderMessage, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand BrowseSoundCommand { get; }

    public Action? CloseAction { get; set; }

    public SettingsViewModel(SettingsService settingsService, AutoStartService autoStartService)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;

        SaveCommand = new RelayCommand(Save);
        BrowseSoundCommand = new RelayCommand(BrowseSound);

        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.CurrentSettings;
        IntervalMinutes = s.IntervalMinutes;
        IdleThresholdMinutes = s.IdleThresholdMinutes;
        ShowToast = s.ShowToast;
        PlaySound = s.PlaySound;
        ShowDialog = s.ShowDialog;
        CustomSoundPath = s.CustomSoundPath;
        AutoStart = _autoStartService.IsAutoStartEnabled();
        ReminderMessage = s.ReminderMessage;
    }

    private void Save()
    {
        var settings = new AppSettings
        {
            IntervalMinutes = Math.Max(1, IntervalMinutes),
            IdleThresholdMinutes = Math.Max(1, IdleThresholdMinutes),
            ShowToast = ShowToast,
            PlaySound = PlaySound,
            ShowDialog = ShowDialog,
            CustomSoundPath = CustomSoundPath,
            AutoStart = AutoStart,
            ReminderMessage = ReminderMessage
        };

        _settingsService.Save(settings);
        _autoStartService.SetAutoStart(AutoStart);
        CloseAction?.Invoke();
    }

    private void BrowseSound()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*",
            Title = "Select notification sound"
        };

        if (dialog.ShowDialog() == true)
        {
            CustomSoundPath = dialog.FileName;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
