using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathForge.Desktop.Services;
using PathForge.Desktop.ViewModels;
using PathForge.Application.History;
using PathForge.Infrastructure.DependencyInjection;
using PathForge.Infrastructure.Persistence;
using System.IO;
using System.Windows;

namespace PathForge.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PathForge");
            var evidenceDirectory = Path.Combine(dataDirectory, "Evidence");
            Directory.CreateDirectory(dataDirectory);
            Directory.CreateDirectory(evidenceDirectory);

            var databasePath = Path.Combine(dataDirectory, "pathforge.db");
            var connectionString = $"Data Source={databasePath};Foreign Keys=True";

            var services = new ServiceCollection();
            services.AddPathForgeInfrastructure(connectionString, evidenceDirectory);
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<IEvidenceFilePicker, WpfEvidenceFilePicker>();
            services.AddSingleton<IEvidenceOpener, ShellEvidenceOpener>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ProblemsViewModel>();
            services.AddSingleton<WorkSessionViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            _serviceProvider = services.BuildServiceProvider();

            var factory = _serviceProvider.GetRequiredService<IDbContextFactory<PathForgeDbContext>>();
            await using (var context = await factory.CreateDbContextAsync())
                await context.Database.MigrateAsync();

            var window = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "PathForge chưa thể khởi động hoặc mở cơ sở dữ liệu. Dữ liệu hiện có vẫn được giữ nguyên.\n\n" + exception.Message,
                "Không thể khởi động PathForge", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
