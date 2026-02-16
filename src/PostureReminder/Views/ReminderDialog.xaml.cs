using System.Windows;

namespace PostureReminder.Views;

public partial class ReminderDialog : Window
{
    public ReminderDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
