using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Threading;
using PostureReminder.Services;
using PostureReminder.ViewModels;
using PostureReminder.Views;
using WinForms = System.Windows.Forms;

namespace PostureReminder.Tray;

public class TrayIconManager : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly WinForms.ToolStripMenuItem _pauseResumeItem;
    private readonly WinForms.ToolStripMenuItem _statusItem;
    private readonly SettingsService _settingsService;
    private readonly ReminderService _reminderService;
    private readonly AutoStartService _autoStartService;
    private readonly IdleDetectionService _idleDetectionService;
    private readonly DispatcherTimer _tooltipTimer;
    private SettingsWindow? _settingsWindow;

    public TrayIconManager(
        SettingsService settingsService,
        ReminderService reminderService,
        AutoStartService autoStartService,
        IdleDetectionService idleDetectionService)
    {
        _settingsService = settingsService;
        _reminderService = reminderService;
        _autoStartService = autoStartService;
        _idleDetectionService = idleDetectionService;

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "PostureReminder",
            Visible = true
        };

        _statusItem = new WinForms.ToolStripMenuItem("Status: Running")
        {
            Enabled = false
        };

        _pauseResumeItem = new WinForms.ToolStripMenuItem("一時停止", null, OnPauseResumeClick);

        var settingsItem = new WinForms.ToolStripMenuItem("設定...", null, OnSettingsClick);
        var resetItem = new WinForms.ToolStripMenuItem("タイマーリセット", null, OnResetClick);
        var exitItem = new WinForms.ToolStripMenuItem("終了", null, OnExitClick);

        _notifyIcon.ContextMenuStrip = new WinForms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add(_statusItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(_pauseResumeItem);
        _notifyIcon.ContextMenuStrip.Items.Add(resetItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(settingsItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(exitItem);

        _notifyIcon.DoubleClick += (_, _) => OnSettingsClick(null, EventArgs.Empty);

        _reminderService.StateChanged += OnReminderStateChanged;

        _tooltipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tooltipTimer.Tick += OnTooltipTimerTick;
        _tooltipTimer.Start();

        UpdateUI();
    }

    private static Icon LoadIcon()
    {
        var resourcePath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "Resources", "tray.ico");

        if (System.IO.File.Exists(resourcePath))
        {
            return new Icon(resourcePath);
        }

        return SystemIcons.Application;
    }

    private void OnPauseResumeClick(object? sender, EventArgs e)
    {
        if (_reminderService.State == ReminderState.Running)
        {
            _reminderService.PauseByUser();
        }
        else if (_reminderService.State == ReminderState.PausedByUser)
        {
            _reminderService.ResumeByUser();
        }
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        _reminderService.Stop();
        _reminderService.Start();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        if (_settingsWindow is null || !_settingsWindow.IsLoaded)
        {
            var vm = new SettingsViewModel(_settingsService, _autoStartService);
            _settingsWindow = new SettingsWindow(vm);
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        // Force-close settings window if open
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closing -= null!;
            _settingsWindow.Close();
        }

        Application.Current.Shutdown();
    }

    private void OnReminderStateChanged()
    {
        Application.Current.Dispatcher.Invoke(UpdateUI);
    }

    private void OnTooltipTimerTick(object? sender, EventArgs e)
    {
        UpdateTooltip();
        UpdateIconWithTime();
    }

    private void UpdateUI()
    {
        var state = _reminderService.State;

        _pauseResumeItem.Text = state switch
        {
            ReminderState.PausedByUser => "再開",
            _ => "一時停止"
        };

        _pauseResumeItem.Enabled = state is ReminderState.Running or ReminderState.PausedByUser;

        _statusItem.Text = state switch
        {
            ReminderState.Running => "動作中",
            ReminderState.PausedByUser => "一時停止中 (手動)",
            ReminderState.PausedByIdle => "一時停止中 (離席)",
            ReminderState.PausedByDialog => "通知中...",
            _ => "停止中"
        };

        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        var remaining = _reminderService.Remaining;
        var text = _reminderService.State switch
        {
            ReminderState.Running => $"PostureReminder - 残り {remaining.Minutes:D2}:{remaining.Seconds:D2}",
            ReminderState.PausedByUser => "PostureReminder - 一時停止中",
            ReminderState.PausedByIdle => "PostureReminder - 離席中",
            _ => "PostureReminder"
        };

        // NotifyIcon.Text has a 127 char limit
        _notifyIcon.Text = text.Length > 127 ? text[..127] : text;
    }

    private int _lastDisplayedMinutes = -1;

    private void UpdateIconWithTime()
    {
        var state = _reminderService.State;
        var remaining = _reminderService.Remaining;
        int minutes = (int)Math.Ceiling(remaining.TotalMinutes);

        // Paused states show special icons
        if (state is ReminderState.PausedByUser or ReminderState.PausedByIdle)
        {
            if (_lastDisplayedMinutes != -2)
            {
                _lastDisplayedMinutes = -2;
                _notifyIcon.Icon?.Dispose();
                _notifyIcon.Icon = CreateTextIcon("||", Color.FromArgb(255, 152, 0));
            }
            return;
        }

        if (state != ReminderState.Running)
            return;

        // Only redraw when the displayed number changes
        if (minutes == _lastDisplayedMinutes)
            return;

        _lastDisplayedMinutes = minutes;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Icon = CreateTextIcon(minutes.ToString(), Color.FromArgb(76, 175, 80));
    }

    private static Icon CreateTextIcon(string text, Color bgColor)
    {
        const int size = 16;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        // Draw rounded rectangle background
        using (var bgBrush = new SolidBrush(bgColor))
        {
            var path = new GraphicsPath();
            const int r = 3;
            var rect = new Rectangle(0, 0, size - 1, size - 1);
            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(bgBrush, path);
        }

        // Choose font size based on text length
        float fontSize = text.Length switch
        {
            1 => 12f,
            2 => 9.5f,
            _ => 7f
        };

        using var font = new Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.White);

        var textSize = g.MeasureString(text, font);
        float x = (size - textSize.Width) / 2f;
        float y = (size - textSize.Height) / 2f;
        g.DrawString(text, font, brush, x, y);

        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _tooltipTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
