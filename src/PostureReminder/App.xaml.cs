using System.Windows;
using PostureReminder.Models;
using PostureReminder.Services;
using PostureReminder.Tray;

namespace PostureReminder;

public partial class App : Application
{
    private TrayIconManager? _trayIconManager;
    private SettingsService? _settingsService;
    private ReminderService? _reminderService;
    private NotificationService? _notificationService;
    private IdleDetectionService? _idleDetectionService;
    private AutoStartService? _autoStartService;
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _mutex = new Mutex(true, "PostureReminder_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("PostureReminder is already running.", "PostureReminder",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settingsService = new SettingsService();
        _autoStartService = new AutoStartService();
        _notificationService = new NotificationService();
        _idleDetectionService = new IdleDetectionService();
        _reminderService = new ReminderService(_settingsService, _notificationService, _idleDetectionService);
        _trayIconManager = new TrayIconManager(_settingsService, _reminderService, _autoStartService, _idleDetectionService);

        _reminderService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _reminderService?.Stop();
        _trayIconManager?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
