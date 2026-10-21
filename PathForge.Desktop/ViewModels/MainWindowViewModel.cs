namespace PathForge.Desktop.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private object _currentViewModel;

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        ProblemsViewModel problems,
        WorkSessionViewModel workSession,
        HistoryViewModel history)
    {
        Dashboard = dashboard;
        Problems = problems;
        WorkSession = workSession;
        History = history;
        _currentViewModel = Dashboard;

        Dashboard.CreateProblemRequested += ShowNewProblem;
        Dashboard.OpenProblemRequested += ShowProblem;
        Dashboard.OpenSessionRequested += ShowWorkSession;
        Dashboard.OpenWorkLogRequested += OpenWorkLog;
        Problems.ProblemChanged += () => _ = Dashboard.RefreshAsync();
        Problems.OpenWorkLogRequested += OpenWorkLog;
        WorkSession.SessionChanged += () => _ = Dashboard.RefreshAsync();
        History.OpenResultRequested += OpenHistoryResult;

        ShowDashboardCommand = new RelayCommand(() =>
        {
            CurrentViewModel = Dashboard;
            _ = Dashboard.RefreshAsync();
        });
        ShowProblemsCommand = new RelayCommand(() => _ = ShowProblemsAsync());
        ShowPracticeCommand = new RelayCommand(() =>
        {
            CurrentViewModel = WorkSession;
            _ = WorkSession.RefreshAsync();
        });
        ShowHistoryCommand = new RelayCommand(() => CurrentViewModel = History);
        _ = Dashboard.RefreshAsync();
    }

    public DashboardViewModel Dashboard { get; }
    public ProblemsViewModel Problems { get; }
    public WorkSessionViewModel WorkSession { get; }
    public HistoryViewModel History { get; }
    public object CurrentViewModel { get => _currentViewModel; private set => SetProperty(ref _currentViewModel, value); }
    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowProblemsCommand { get; }
    public RelayCommand ShowPracticeCommand { get; }
    public RelayCommand ShowHistoryCommand { get; }

    private async Task ShowProblemsAsync()
    {
        CurrentViewModel = Problems;
        await Problems.RefreshAsync();
    }

    private async void ShowNewProblem()
    {
        CurrentViewModel = Problems;
        await Problems.RefreshAsync();
        Problems.FocusNewProblem();
    }

    private void ShowProblem(int id)
    {
        CurrentViewModel = Problems;
        _ = Problems.RefreshAsync(id);
    }

    private void ShowWorkSession()
    {
        CurrentViewModel = WorkSession;
        _ = WorkSession.RefreshAsync();
    }

    private void OpenHistoryResult(PathForge.Application.History.HistorySearchResult result)
    {
        if (result.WorkLogId.HasValue)
        {
            CurrentViewModel = WorkSession;
            _ = WorkSession.LoadHistoryWorkLogAsync(result.WorkLogId.Value);
            return;
        }
        CurrentViewModel = Problems;
        _ = Problems.OpenProblemAsync(result.ProblemId, result.ScopeItemId);
    }

    private void OpenWorkLog(int id)
    {
        CurrentViewModel = WorkSession;
        _ = WorkSession.LoadHistoryWorkLogAsync(id);
    }
}
