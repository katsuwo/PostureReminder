namespace PostureReminder.Models;

public class AppSettings
{
    public int IntervalMinutes { get; set; } = 30;
    public int IdleThresholdMinutes { get; set; } = 5;
    public bool ShowToast { get; set; } = true;
    public bool PlaySound { get; set; } = true;
    public bool ShowDialog { get; set; } = true;
    public string? CustomSoundPath { get; set; }
    public bool AutoStart { get; set; } = false;
    public string ReminderMessage { get; set; } = "姿勢を変えましょう！立ち上がってストレッチしてください。";
}
