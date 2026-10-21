namespace PathForge.Desktop.Services;

public interface IUserDialogService
{
    bool Confirm(string title, string message);
    void ShowMessage(string title, string message, bool isError = false);
}
