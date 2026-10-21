using System.Windows;

namespace PathForge.Desktop.Services;

public sealed class WpfUserDialogService : IUserDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public UnsavedChangesChoice AskToSaveChanges(string itemName) =>
        MessageBox.Show(
            $"{itemName} có thay đổi chưa lưu. Chọn Có để lưu, Không để bỏ thay đổi, hoặc Hủy để tiếp tục chỉnh sửa.",
            "Thay đổi chưa được lưu", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => UnsavedChangesChoice.Save,
            MessageBoxResult.No => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };

    public void ShowMessage(string title, string message, bool isError = false) =>
        MessageBox.Show(message, title, MessageBoxButton.OK,
            isError ? MessageBoxImage.Warning : MessageBoxImage.Information);
}
