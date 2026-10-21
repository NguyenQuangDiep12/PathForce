using System.Windows;

namespace PathForge.Desktop.Services;

public sealed class WpfUserDialogService : IUserDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowMessage(string title, string message, bool isError = false) =>
        MessageBox.Show(message, title, MessageBoxButton.OK,
            isError ? MessageBoxImage.Warning : MessageBoxImage.Information);
}
