namespace PathForge.Desktop.Services;

public interface IUserDialogService
{
    bool Confirm(string title, string message);
    UnsavedChangesChoice AskToSaveChanges(string itemName);
    void ShowMessage(string title, string message, bool isError = false);
}
