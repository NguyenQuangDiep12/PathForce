using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathForge.Desktop.Services;
using PathForge.Desktop.ViewModels;
using PathForge.Application.History;
using PathForge.Infrastructure.DependencyInjection;
using PathForge.Infrastructure.Persistence;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PathForge.Desktop;

public partial class App : System.Windows.Application
{
    private const string BundledDatabaseResourceName = "PathForge.Desktop.Resources.pathforge.db";
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var configuredDataDirectory = Environment.GetEnvironmentVariable("PATHFORGE_DATA_DIRECTORY");
            var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PathForge")
                : Path.GetFullPath(configuredDataDirectory);
            var evidenceDirectory = Path.Combine(dataDirectory, "Evidence");
            var knowledgeImagesDirectory = Path.Combine(dataDirectory, "KnowledgeImages");
            Directory.CreateDirectory(dataDirectory);
            Directory.CreateDirectory(evidenceDirectory);
            Directory.CreateDirectory(knowledgeImagesDirectory);

            var databasePath = Path.Combine(dataDirectory, "pathforge.db");
            await CopyBundledDatabaseIfMissingAsync(databasePath);
            var connectionString = $"Data Source={databasePath};Foreign Keys=True";

            var services = new ServiceCollection();
            services.AddPathForgeInfrastructure(connectionString, evidenceDirectory, knowledgeImagesDirectory);
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<IEvidenceFilePicker, WpfEvidenceFilePicker>();
            services.AddSingleton<IEvidenceOpener, ShellEvidenceOpener>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ProblemsViewModel>();
            services.AddSingleton<WorkSessionViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<KnowledgeDocumentsViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            _serviceProvider = services.BuildServiceProvider();

            var factory = _serviceProvider.GetRequiredService<IDbContextFactory<PathForgeDbContext>>();
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            }

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

    private static async Task CopyBundledDatabaseIfMissingAsync(string databasePath)
    {
        if (File.Exists(databasePath))
            return;

        var temporaryPath = $"{databasePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var resource = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(BundledDatabaseResourceName)
                ?? throw new InvalidOperationException("Không tìm thấy SQLite database được nhúng trong ứng dụng.");

            await using (resource)
            await using (var destination = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await resource.CopyToAsync(destination);
            }

            File.Move(temporaryPath, databasePath);
        }
        catch (IOException) when (File.Exists(databasePath))
        {
            // Another app instance created the local database after the initial check.
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
