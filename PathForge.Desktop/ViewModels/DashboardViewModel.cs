using PathForge.Application.Problems;
using PathForge.Application.WorkLogs;
using PathForge.Domain.Enums;

namespace PathForge.Desktop.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IProblemService _problemService;
    private readonly IWorkLogService _workLogService;
    private ProblemDetails? _currentProblem;
    private WorkLogDetails? _openSession;
    private WorkLogDetails? _recentReport;

    public DashboardViewModel(IProblemService problemService, IWorkLogService workLogService)
    {
        _problemService = problemService;
        _workLogService = workLogService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CreateProblemCommand = new RelayCommand(() => CreateProblemRequested?.Invoke());
        OpenProblemCommand = new RelayCommand(() =>
        {
            if (CurrentProblem is not null)
                OpenProblemRequested?.Invoke(CurrentProblem.Id);
        }, () => CurrentProblem is not null);
        OpenSessionCommand = new RelayCommand(() => OpenSessionRequested?.Invoke(), () => OpenSession is not null);
        OpenRecentReportCommand = new RelayCommand(() =>
        {
            if (RecentReport is not null)
                OpenWorkLogRequested?.Invoke(RecentReport.Id);
        }, () => RecentReport is not null);
    }

    public event Action? CreateProblemRequested;
    public event Action<int>? OpenProblemRequested;
    public event Action? OpenSessionRequested;
    public event Action<int>? OpenWorkLogRequested;
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand CreateProblemCommand { get; }
    public RelayCommand OpenProblemCommand { get; }
    public RelayCommand OpenSessionCommand { get; }
    public RelayCommand OpenRecentReportCommand { get; }

    public ProblemDetails? CurrentProblem
    {
        get => _currentProblem;
        private set
        {
            if (!SetProperty(ref _currentProblem, value))
                return;
            OnPropertyChanged(nameof(HasProblem));
            OnPropertyChanged(nameof(StatusText));
            OpenProblemCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasProblem => CurrentProblem is not null;
    public WorkLogDetails? OpenSession
    {
        get => _openSession;
        private set
        {
            if (!SetProperty(ref _openSession, value))
                return;
            OnPropertyChanged(nameof(HasOpenSession));
            OnPropertyChanged(nameof(OpenSessionText));
            OpenSessionCommand.NotifyCanExecuteChanged();
        }
    }
    public bool HasOpenSession => OpenSession is not null;
    public WorkLogDetails? RecentReport
    {
        get => _recentReport;
        private set
        {
            if (!SetProperty(ref _recentReport, value))
                return;
            OnPropertyChanged(nameof(HasRecentReport));
            OnPropertyChanged(nameof(RecentReportSummary));
            OpenRecentReportCommand.NotifyCanExecuteChanged();
        }
    }
    public bool HasRecentReport => RecentReport is not null;
    public string RecentReportSummary => RecentReport?.Summary ?? string.Empty;
    public string OpenSessionText => OpenSession?.Status switch
    {
        WorkLogStatus.Running => "Có phiên đang chạy",
        WorkLogStatus.Paused => "Có phiên đang tạm dừng",
        WorkLogStatus.AwaitingReport => "Có phiên đang chờ báo cáo",
        _ => string.Empty
    };
    public string StatusText => CurrentProblem?.Status switch
    {
        ProblemStatus.NotStarted => "Chưa bắt đầu",
        ProblemStatus.InProgress => "Đang thực hiện",
        ProblemStatus.Blocked => "Bị chặn",
        ProblemStatus.Resolved => "Đã hoàn thành",
        _ => string.Empty
    };

    public async Task RefreshAsync()
    {
        CurrentProblem = await _problemService.GetCurrentAsync();
        OpenSession = await _workLogService.GetOpenAsync();
        if (CurrentProblem is null)
            RecentReport = null;
        else
            RecentReport = (await _workLogService.GetByProblemAsync(CurrentProblem.Id))
                .FirstOrDefault(log => log.Status == WorkLogStatus.Completed);
    }
}
