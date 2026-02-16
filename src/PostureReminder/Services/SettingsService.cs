using System.IO;
using System.Text.Json;
using PostureReminder.Models;

namespace PostureReminder.Services;

public class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PostureReminder");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings CurrentSettings { get; private set; }

    public event Action? SettingsChanged;

    public SettingsService()
    {
        CurrentSettings = Load();
    }

    public void Save(AppSettings settings)
    {
        CurrentSettings = settings;
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        SettingsChanged?.Invoke();
    }

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Fall back to defaults on any error
        }
        return new AppSettings();
    }
}
