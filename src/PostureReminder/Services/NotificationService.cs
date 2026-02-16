using System.IO;
using System.Media;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using PostureReminder.Views;

namespace PostureReminder.Services;

public class NotificationService
{
    public void ShowToast(string message)
    {
        new ToastContentBuilder()
            .AddText("PostureReminder")
            .AddText(message)
            .Show();
    }

    public void PlaySound(string? customSoundPath)
    {
        if (!string.IsNullOrEmpty(customSoundPath) && File.Exists(customSoundPath))
        {
            if (customSoundPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                using var player = new SoundPlayer(customSoundPath);
                player.Play();
            }
            else
            {
                var player = new System.Windows.Media.MediaPlayer();
                player.Open(new Uri(customSoundPath));
                player.Play();
            }
        }
        else
        {
            SystemSounds.Exclamation.Play();
        }
    }

    public void ShowReminderDialog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ReminderDialog(message);
            dialog.ShowDialog();
        });
    }
}
