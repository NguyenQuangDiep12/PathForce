namespace PathForge.Desktop.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly PathForge.Desktop.Services.IUserDialogService _dialogs;
    private object _currentViewModel;

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        ProblemsViewModel problems,
        WorkSessionViewModel workSession,
        HistoryViewModel history,
        KnowledgeDocumentsViewModel knowledgeDocuments,
        PathForge.Desktop.Services.IUserDialogService dialogs)
    {
        _dialogs = dialogs;
        Dashboard = dashboard;
        Problems = problems;
        WorkSession = workSession;
        History = history;
        KnowledgeDocuments = knowledgeDocuments;
        _currentViewModel = Dashboard;

        Dashboard.CreateProblemRequested += ShowNewProblem;
        Dashboard.OpenProblemRequested += ShowProblem;
        Dashboard.OpenSessionRequested += ShowWorkSession;
        Dashboard.ContinueWorkRequested += ShowWorkSession;
        Dashboard.OpenWorkLogRequested += OpenWorkLog;
        Problems.ProblemChanged += () => _ = Dashboard.RefreshAsync();
        Problems.OpenWorkLogRequested += OpenWorkLog;
        WorkSession.SessionChanged += () => _ = Dashboard.RefreshAsync();
        WorkSession.OpenProblemRequested += ShowProblem;
        History.OpenResultRequested += OpenHistoryResult;

        ShowDashboardCommand = new RelayCommand(() =>
        {
            _ = NavigateAsync(async () =>
            {
                CurrentViewModel = Dashboard;
                await Dashboard.RefreshAsync();
            });
        });
        ShowProblemsCommand = new RelayCommand(() => _ = ShowProblemsAsync());
        ShowPracticeCommand = new RelayCommand(() => _ = NavigateAsync(async () =>
        {
            CurrentViewModel = WorkSession;
            await WorkSession.RefreshAsync();
        }));
        ShowHistoryCommand = new RelayCommand(() => _ = NavigateAsync(() =>
        {
            CurrentViewModel = History;
            return Task.CompletedTask;
        }));
        ShowKnowledgeDocumentsCommand = new RelayCommand(() => _ = NavigateAsync(async () =>
        {
            CurrentViewModel = KnowledgeDocuments;
            await KnowledgeDocuments.SearchAsync();
        }));
        _ = Dashboard.RefreshAsync();
    }

    public DashboardViewModel Dashboard { get; }
    public ProblemsViewModel Problems { get; }
    public WorkSessionViewModel WorkSession { get; }
    public HistoryViewModel History { get; }
    public KnowledgeDocumentsViewModel KnowledgeDocuments { get; }
    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (!SetProperty(ref _currentViewModel, value))
                return;
            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsProblemsActive));
            OnPropertyChanged(nameof(IsPracticeActive));
            OnPropertyChanged(nameof(IsHistoryActive));
            OnPropertyChanged(nameof(IsKnowledgeDocumentsActive));
        }
    }
    public bool IsDashboardActive => ReferenceEquals(CurrentViewModel, Dashboard);
    public bool IsProblemsActive => ReferenceEquals(CurrentViewModel, Problems);
    public bool IsPracticeActive => ReferenceEquals(CurrentViewModel, WorkSession);
    public bool IsHistoryActive => ReferenceEquals(CurrentViewModel, History);
    public bool IsKnowledgeDocumentsActive => ReferenceEquals(CurrentViewModel, KnowledgeDocuments);
    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowProblemsCommand { get; }
    public RelayCommand ShowPracticeCommand { get; }
    public RelayCommand ShowHistoryCommand { get; }
    public RelayCommand ShowKnowledgeDocumentsCommand { get; }

    private async Task ShowProblemsAsync()
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        CurrentViewModel = Problems;
        await Problems.RefreshAsync();
    }

    private async void ShowNewProblem()
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        CurrentViewModel = Problems;
        await Problems.RefreshAsync();
        Problems.FocusNewProblem();
    }

    private void ShowProblem(int id) => _ = ShowProblemAsync(id, null);

    private async void ShowProblem(int id, int? scopeItemId)
    {
        await ShowProblemAsync(id, scopeItemId);
    }

    private async Task ShowProblemAsync(int id, int? scopeItemId)
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        CurrentViewModel = Problems;
        await Problems.OpenProblemAsync(id, scopeItemId);
    }

    private async void ShowWorkSession()
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        CurrentViewModel = WorkSession;
        await WorkSession.RefreshAsync();
    }

    private async void OpenHistoryResult(PathForge.Application.History.HistorySearchResult result)
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        if (result.WorkLogId.HasValue)
        {
            CurrentViewModel = WorkSession;
            _ = WorkSession.LoadHistoryWorkLogAsync(result.WorkLogId.Value);
            return;
        }
        CurrentViewModel = Problems;
        _ = Problems.OpenProblemAsync(result.ProblemId, result.ScopeItemId);
    }

    private async void OpenWorkLog(int id)
    {
        if (!await CanLeaveCurrentViewAsync())
            return;
        CurrentViewModel = WorkSession;
        _ = WorkSession.LoadHistoryWorkLogAsync(id);
    }

    private async Task NavigateAsync(Func<Task> navigation)
    {
        if (await CanLeaveCurrentViewAsync())
            await navigation();
    }

    private async Task<bool> CanLeaveCurrentViewAsync(bool refreshAfterSave = true)
    {
        if (ReferenceEquals(CurrentViewModel, Problems) && Problems.HasUnsavedChanges)
        {
            var choice = _dialogs.AskToSaveChanges("Thông tin bài toán");
            if (choice == PathForge.Desktop.Services.UnsavedChangesChoice.Cancel)
                return false;
            if (choice == PathForge.Desktop.Services.UnsavedChangesChoice.Discard)
            {
                Problems.DiscardPendingChanges();
                return true;
            }
            return await Problems.SavePendingChangesAsync(refreshAfterSave);
        }

        if (ReferenceEquals(CurrentViewModel, WorkSession) && WorkSession.HasUnsavedChanges)
        {
            var choice = _dialogs.AskToSaveChanges("Phiên làm việc");
            if (choice == PathForge.Desktop.Services.UnsavedChangesChoice.Cancel)
                return false;
            if (choice == PathForge.Desktop.Services.UnsavedChangesChoice.Discard)
            {
                WorkSession.DiscardPendingChanges();
                return true;
            }
            return await WorkSession.SavePendingChangesAsync(refreshAfterSave);
        }
        if (ReferenceEquals(CurrentViewModel, KnowledgeDocuments) && KnowledgeDocuments.IsDirty)
        {
            return await KnowledgeDocuments.CanCloseAsync(refreshAfterSave);
        }
        return true;
    }

    public Task<bool> CanCloseAsync() => CanLeaveCurrentViewAsync(refreshAfterSave: false);
}
