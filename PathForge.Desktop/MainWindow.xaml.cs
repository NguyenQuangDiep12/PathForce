using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using PathForge.Desktop.ViewModels;

namespace PathForge.Desktop;

public partial class MainWindow : Window
{
    private bool _closeApproved;
    private bool _closeCheckInProgress;

    public MainWindow() => InitializeComponent();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeApproved)
            return;

        e.Cancel = true;
        if (_closeCheckInProgress)
            return;

        _closeCheckInProgress = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            try
            {
                if (DataContext is not MainWindowViewModel viewModel || await viewModel.CanCloseAsync())
                {
                    _closeApproved = true;
                    Close();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "PathForge chưa thể đóng vì không xử lý được thay đổi đang chờ. Dữ liệu vẫn còn trên màn hình.\n\n" + exception.Message,
                    "Chưa thể thoát PathForge", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _closeCheckInProgress = false;
            }
        }));
    }
}
