using System.Windows;
using PostureReminder.ViewModels;

namespace PostureReminder.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        viewModel.CloseAction = Hide;
        DataContext = viewModel;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
