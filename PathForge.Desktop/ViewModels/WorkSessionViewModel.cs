using System.Collections.ObjectModel;
using System.Windows.Threading;
using PathForge.Application.Evidence;
using PathForge.Application.Problems;
using PathForge.Application.ScopeItems;
using PathForge.Application.WorkLogs;
using PathForge.Desktop.Services;
using PathForge.Domain.Enums;

namespace PathForge.Desktop.ViewModels;

public sealed class WorkSessionViewModel : ObservableObject, IDisposable
{
    private readonly IProblemService _problemService;
    private readonly IScopeItemService _scopeItemService;
    private readonly IWorkLogService _workLogService;
    private readonly IEvidenceService _evidenceService;
    private readonly IUserDialogService _dialogs;
    private readonly IEvidenceFilePicker _evidenceFilePicker;
    private readonly IEvidenceOpener _evidenceOpener;
    private readonly DispatcherTimer _timer;
    private int _scopeItemsLoadGeneration;
    private ObservableCollection<ProblemListItem> _problems = new();
    private ProblemListItem? _selectedProblem;
    private ScopeItemDetails? _selectedScopeItem;
    private WorkLogDetails? _session;
    private string _taskTitle = string.Empty;
    private string _quickScopeTitle = string.Empty;
    private string _quickScopeReason = string.Empty;
    private ScopeItemStatus _quickScopeStatus = ScopeItemStatus.Backlog;
    private bool _isQuickScopeFormVisible;
    private string _selectedDuration = "25 phút";
    private string _elapsedText = "00:00";
    private string _whatWasDone = string.Empty;
    private string _result = string.Empty;
    private string _learned = string.Empty;
    private string _blocker = string.Empty;
    private string _nextStep = string.Empty;
    private EvidenceDetails? _selectedEvidence;
    private string _evidenceDescription = string.Empty;

    public event Action? SessionChanged;
    public event Action<int, int?>? OpenProblemRequested;

    public WorkSessionViewModel(
        IProblemService problemService,
        IScopeItemService scopeItemService,
        IWorkLogService workLogService,
        IEvidenceService evidenceService,
        IUserDialogService dialogs,
        IEvidenceFilePicker evidenceFilePicker,
        IEvidenceOpener evidenceOpener)
    {
        _problemService = problemService;
        _scopeItemService = scopeItemService;
        _workLogService = workLogService;
        _evidenceService = evidenceService;
        _dialogs = dialogs;
        _evidenceFilePicker = evidenceFilePicker;
        _evidenceOpener = evidenceOpener;
        Durations = new ObservableCollection<string> { "Bỏ qua", "15 phút", "25 phút", "45 phút", "60 phút" };
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        StartCommand = new AsyncRelayCommand(StartAsync, () => Session is null);
        PauseCommand = new AsyncRelayCommand(PauseAsync, () => Session?.Status == WorkLogStatus.Running);
        ResumeCommand = new AsyncRelayCommand(ResumeAsync, () => Session?.Status == WorkLogStatus.Paused);
        EndCommand = new AsyncRelayCommand(EndAsync, () => Session?.Status == WorkLogStatus.Running || Session?.Status == WorkLogStatus.Paused);
        AbandonCommand = new AsyncRelayCommand(AbandonAsync, () => Session?.Status == WorkLogStatus.Running || Session?.Status == WorkLogStatus.Paused);
        SaveReportCommand = new AsyncRelayCommand(SaveReportAsync, () => IsReportVisible);
        SaveTaskTitleCommand = new AsyncRelayCommand(SaveTaskTitleAsync, () => CanUpdateTask);
        ShowQuickScopeCommand = new RelayCommand(() => IsQuickScopeFormVisible = true, () => CanRecordQuickScope);
        SaveQuickScopeCommand = new AsyncRelayCommand(SaveQuickScopeAsync, () => CanRecordQuickScope);
        CancelQuickScopeCommand = new RelayCommand(CancelQuickScope, () => IsQuickScopeFormVisible);
        AddEvidenceCommand = new AsyncRelayCommand(AddEvidenceAsync, () => Session is not null);
        OpenEvidenceCommand = new RelayCommand(OpenSelectedEvidence, () => SelectedEvidence is not null);
        DeleteEvidenceCommand = new AsyncRelayCommand(DeleteSelectedEvidenceAsync, () => SelectedEvidence is not null);
        OpenProblemCommand = new RelayCommand(OpenRelatedProblem, () => Session is not null);
        OpenScopeItemCommand = new RelayCommand(OpenRelatedScopeItem, () => Session?.ScopeItemId.HasValue == true);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateElapsedText();
    }

    public ObservableCollection<ProblemListItem> Problems
    {
        get => _problems;
        private set => SetProperty(ref _problems, value);
    }
    private ObservableCollection<ScopeItemDetails> _scopeItems = new();
    public ObservableCollection<ScopeItemDetails> ScopeItems
    {
        get => _scopeItems;
        private set => SetProperty(ref _scopeItems, value);
    }
    public ObservableCollection<EvidenceDetails> EvidenceItems { get; } = new();
    public ObservableCollection<string> Durations { get; }
    public ObservableCollection<KeyValuePair<ScopeItemStatus, string>> QuickScopeStatuses { get; } = new()
    {
        new(ScopeItemStatus.InScope, "Trong phạm vi"),
        new(ScopeItemStatus.Backlog, "Để sau"),
        new(ScopeItemStatus.Done, "Đã xử lý"),
        new(ScopeItemStatus.Excluded, "Loại khỏi phạm vi")
    };
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand PauseCommand { get; }
    public AsyncRelayCommand ResumeCommand { get; }
    public AsyncRelayCommand EndCommand { get; }
    public AsyncRelayCommand AbandonCommand { get; }
    public AsyncRelayCommand SaveReportCommand { get; }
    public AsyncRelayCommand SaveTaskTitleCommand { get; }
    public RelayCommand ShowQuickScopeCommand { get; }
    public AsyncRelayCommand SaveQuickScopeCommand { get; }
    public RelayCommand CancelQuickScopeCommand { get; }
    public AsyncRelayCommand AddEvidenceCommand { get; }
    public RelayCommand OpenEvidenceCommand { get; }
    public AsyncRelayCommand DeleteEvidenceCommand { get; }
    public RelayCommand OpenProblemCommand { get; }
    public RelayCommand OpenScopeItemCommand { get; }

    public ProblemListItem? SelectedProblem
    {
        get => _selectedProblem;
        set
        {
            if (!SetProperty(ref _selectedProblem, value))
                return;
            var generation = ++_scopeItemsLoadGeneration;
            SelectedScopeItem = null;
            ScopeItems = new ObservableCollection<ScopeItemDetails>();
            if (value is not null)
                _ = LoadScopeItemsAsync(value.Id, generation);
        }
    }

    public ScopeItemDetails? SelectedScopeItem
    {
        get => _selectedScopeItem;
        set
        {
            if (!SetProperty(ref _selectedScopeItem, value) || value is null)
                return;
            TaskTitle = value.Title;
        }
    }

    public WorkLogDetails? Session
    {
        get => _session;
        private set
        {
            if (!SetProperty(ref _session, value))
                return;
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsAwaitingReport));
            OnPropertyChanged(nameof(IsReportVisible));
            OnPropertyChanged(nameof(CanUpdateTask));
            OnPropertyChanged(nameof(CanRecordQuickScope));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(SessionStatusText));
            UpdateElapsedText();
            SyncTimer();
            AddEvidenceCommand.NotifyCanExecuteChanged();
            if (!CanRecordQuickScope && IsQuickScopeFormVisible)
                IsQuickScopeFormVisible = false;
            OpenProblemCommand.NotifyCanExecuteChanged();
            OpenScopeItemCommand.NotifyCanExecuteChanged();
            NotifyCommands();
        }
    }

    public string TaskTitle { get => _taskTitle; set => SetPendingProperty(ref _taskTitle, value); }
    public string QuickScopeTitle { get => _quickScopeTitle; set => SetPendingProperty(ref _quickScopeTitle, value); }
    public string QuickScopeReason { get => _quickScopeReason; set => SetPendingProperty(ref _quickScopeReason, value); }
    public ScopeItemStatus QuickScopeStatus { get => _quickScopeStatus; set => SetPendingProperty(ref _quickScopeStatus, value); }
    public bool IsQuickScopeFormVisible
    {
        get => _isQuickScopeFormVisible;
        set
        {
            if (SetProperty(ref _isQuickScopeFormVisible, value))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
                CancelQuickScopeCommand.NotifyCanExecuteChanged();
            }
        }
    }
    public string SelectedDuration { get => _selectedDuration; set => SetProperty(ref _selectedDuration, value); }
    public string ElapsedText { get => _elapsedText; private set => SetProperty(ref _elapsedText, value); }
    public string WhatWasDone { get => _whatWasDone; set => SetPendingProperty(ref _whatWasDone, value); }
    public string Result { get => _result; set => SetPendingProperty(ref _result, value); }
    public string Learned { get => _learned; set => SetPendingProperty(ref _learned, value); }
    public string Blocker { get => _blocker; set => SetPendingProperty(ref _blocker, value); }
    public string NextStep { get => _nextStep; set => SetPendingProperty(ref _nextStep, value); }
    public string EvidenceDescription { get => _evidenceDescription; set => SetPendingProperty(ref _evidenceDescription, value); }
    public bool IsIdle => Session is null;
    public bool HasSelectedEvidence => SelectedEvidence is not null;
    public bool IsRunning => Session?.Status == WorkLogStatus.Running;
    public bool IsPaused => Session?.Status == WorkLogStatus.Paused;
    public bool IsAwaitingReport => Session?.Status == WorkLogStatus.AwaitingReport;
    public bool IsReportVisible => Session?.Status == WorkLogStatus.AwaitingReport || Session?.Status == WorkLogStatus.Completed;
    public bool CanUpdateTask => Session?.Status == WorkLogStatus.Running || Session?.Status == WorkLogStatus.Paused;
    public bool CanRecordQuickScope => CanUpdateTask;
    public bool HasUnsavedChanges => Session is not null &&
        ((CanUpdateTask && !Same(TaskTitle, Session.TaskTitle)) ||
         (IsReportVisible && ReportHasChanges()) ||
         (IsQuickScopeFormVisible && (!string.IsNullOrWhiteSpace(QuickScopeTitle) ||
            !string.IsNullOrWhiteSpace(QuickScopeReason) || QuickScopeStatus != ScopeItemStatus.Backlog)) ||
         !string.IsNullOrWhiteSpace(EvidenceDescription));
    public string SessionStatusText => Session?.Status switch
    {
        WorkLogStatus.Running => "Đang thực hiện",
        WorkLogStatus.Paused => "Đang tạm dừng",
        WorkLogStatus.AwaitingReport => "Chờ báo cáo",
        WorkLogStatus.Completed => "Đã lưu",
        WorkLogStatus.Abandoned => "Đã bỏ",
        _ => string.Empty
    };

    public EvidenceDetails? SelectedEvidence
    {
        get => _selectedEvidence;
        set
        {
            if (!SetProperty(ref _selectedEvidence, value))
                return;
            OnPropertyChanged(nameof(HasSelectedEvidence));
            OpenEvidenceCommand.NotifyCanExecuteChanged();
            DeleteEvidenceCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task RefreshAsync()
    {
        var problems = await _problemService.SearchAsync(null);
        Problems = new ObservableCollection<ProblemListItem>(
            problems.Where(item => item.Status != ProblemStatus.Resolved).Take(200));

        var openSession = await _workLogService.GetOpenAsync();
        Session = openSession;
        if (openSession is not null)
        {
            _selectedProblem = Problems.FirstOrDefault(problem => problem.Id == openSession.ProblemId);
            OnPropertyChanged(nameof(SelectedProblem));
            var generation = ++_scopeItemsLoadGeneration;
            await LoadScopeItemsAsync(openSession.ProblemId, generation);
            SelectedScopeItem = ScopeItems.FirstOrDefault(item => item.Id == openSession.ScopeItemId);
            TaskTitle = openSession.TaskTitle;
            WhatWasDone = openSession.WhatWasDone ?? string.Empty;
            Result = openSession.Result ?? string.Empty;
            Learned = openSession.Learned ?? string.Empty;
            Blocker = openSession.Blocker ?? string.Empty;
            NextStep = openSession.NextStep ?? string.Empty;
            await LoadEvidenceAsync(openSession);
        }
        else
        {
            EvidenceItems.Clear();
            SelectedEvidence = null;
            SelectedProblem ??= Problems.FirstOrDefault();
            TaskTitle = string.Empty;
            WhatWasDone = string.Empty;
            Result = string.Empty;
            Learned = string.Empty;
            Blocker = string.Empty;
            NextStep = string.Empty;
        }
    }

    public async Task LoadHistoryWorkLogAsync(int workLogId)
    {
        ++_scopeItemsLoadGeneration;
        var workLog = await _workLogService.GetAsync(workLogId);
        if (workLog is null)
            return;
        Session = workLog;
        _selectedProblem = Problems.FirstOrDefault(problem => problem.Id == workLog.ProblemId);
        OnPropertyChanged(nameof(SelectedProblem));
        TaskTitle = workLog.TaskTitle;
        WhatWasDone = workLog.WhatWasDone ?? string.Empty;
        Result = workLog.Result ?? string.Empty;
        Learned = workLog.Learned ?? string.Empty;
        Blocker = workLog.Blocker ?? string.Empty;
        NextStep = workLog.NextStep ?? string.Empty;
        await LoadEvidenceAsync(workLog);
    }

    private async Task LoadEvidenceAsync(WorkLogDetails workLog)
    {
        var items = await _evidenceService.GetByProblemAsync(workLog.ProblemId);
        EvidenceItems.Clear();
        foreach (var item in items.Where(item => item.WorkLogId == workLog.Id))
            EvidenceItems.Add(item);
        SelectedEvidence = null;
    }

    private async Task LoadSessionDetailsAsync(int id)
    {
        var workLog = await _workLogService.GetAsync(id);
        if (workLog is null)
            return;
        Session = workLog;
        TaskTitle = workLog.TaskTitle;
        WhatWasDone = workLog.WhatWasDone ?? string.Empty;
        Result = workLog.Result ?? string.Empty;
        Learned = workLog.Learned ?? string.Empty;
        Blocker = workLog.Blocker ?? string.Empty;
        NextStep = workLog.NextStep ?? string.Empty;
        await LoadEvidenceAsync(workLog);
    }

    private async Task LoadScopeItemsAsync(int problemId, int generation)
    {
        var items = await _scopeItemService.GetByProblemAsync(problemId);
        if (generation != _scopeItemsLoadGeneration || SelectedProblem?.Id != problemId)
            return;
        ScopeItems = new ObservableCollection<ScopeItemDetails>(
            items.Where(item => item.Status == ScopeItemStatus.InScope));
    }

    private async Task StartAsync()
    {
        if (SelectedProblem is null)
        {
            _dialogs.ShowMessage("Chọn bài toán", "Hãy chọn một bài toán trước khi bắt đầu phiên.");
            return;
        }
        var duration = SelectedDuration == "Bỏ qua" ? (int?)null :
            int.Parse(SelectedDuration.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        var result = await _workLogService.StartAsync(SelectedProblem.Id, TaskTitle, duration, SelectedScopeItem?.Id);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa bắt đầu được phiên", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync();
        SessionChanged?.Invoke();
    }

    private async Task SaveTaskTitleAsync()
    {
        if (Session is null)
            return;
        var id = Session.Id;
        var result = await _workLogService.UpdateTaskTitleAsync(id, TaskTitle);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa cập nhật được nhiệm vụ", result.Error ?? "Nội dung vẫn còn trên màn hình. Vui lòng thử lại.", true);
            return;
        }
        await LoadSessionDetailsAsync(id);
        SessionChanged?.Invoke();
    }

    private async Task SaveQuickScopeAsync()
    {
        if (Session is null)
            return;
        var result = await _scopeItemService.SaveAsync(Session.ProblemId, null,
            new ScopeItemDraft(QuickScopeTitle, QuickScopeReason, QuickScopeStatus));
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa ghi nhận được chủ đề", result.Error ?? "Nội dung vẫn còn trên màn hình. Vui lòng thử lại.", true);
            return;
        }

        QuickScopeTitle = string.Empty;
        QuickScopeReason = string.Empty;
        QuickScopeStatus = ScopeItemStatus.Backlog;
        IsQuickScopeFormVisible = false;
        await LoadScopeItemsAsync(Session.ProblemId, _scopeItemsLoadGeneration);
    }

    private void CancelQuickScope()
    {
        QuickScopeTitle = string.Empty;
        QuickScopeReason = string.Empty;
        QuickScopeStatus = ScopeItemStatus.Backlog;
        IsQuickScopeFormVisible = false;
    }

    private void OpenRelatedProblem()
    {
        if (Session is not null)
            OpenProblemRequested?.Invoke(Session.ProblemId, null);
    }

    private void OpenRelatedScopeItem()
    {
        if (Session?.ScopeItemId is int scopeItemId)
            OpenProblemRequested?.Invoke(Session.ProblemId, scopeItemId);
    }

    private Task PauseAsync() => RunSessionActionAsync(_workLogService.PauseAsync, "Tạm dừng phiên");
    private Task ResumeAsync() => RunSessionActionAsync(_workLogService.ResumeAsync, "Tiếp tục phiên");
    private Task EndAsync() => RunSessionActionAsync(_workLogService.EndAsync, "Kết thúc phiên");

    private async Task AbandonAsync()
    {
        if (Session is null || !_dialogs.Confirm("Bỏ phiên", "Phiên sẽ được lưu là đã bỏ và không có báo cáo. Bạn có muốn tiếp tục?"))
            return;
        var result = await _workLogService.AbandonAsync(Session.Id);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa bỏ được phiên", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync();
        SessionChanged?.Invoke();
    }

    private async Task SaveReportAsync()
    {
        if (Session is null)
            return;
        var id = Session.Id;
        var result = await _workLogService.SaveReportAsync(id,
            new WorkLogReportDraft(WhatWasDone, Result, Learned, Blocker, NextStep));
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa lưu được báo cáo", result.Error ?? "Nội dung vẫn còn trên màn hình. Vui lòng thử lại.", true);
            return;
        }
        await LoadSessionDetailsAsync(id);
        SessionChanged?.Invoke();
    }

    public async Task<bool> SavePendingChangesAsync(bool refreshAfterSave = true)
    {
        if (Session is null || !HasUnsavedChanges)
            return true;
        if (!string.IsNullOrWhiteSpace(EvidenceDescription))
        {
            _dialogs.ShowMessage("Chưa lưu mô tả ảnh", "Chọn ảnh để đính kèm mô tả này hoặc xóa mô tả trước khi rời màn hình.", true);
            return false;
        }

        var id = Session.Id;
        var savedData = false;
        if (CanUpdateTask && !Same(TaskTitle, Session.TaskTitle))
        {
            var taskResult = await _workLogService.UpdateTaskTitleAsync(id, TaskTitle);
            if (!taskResult.Succeeded)
            {
                _dialogs.ShowMessage("Chưa cập nhật được nhiệm vụ", taskResult.Error ?? "Nội dung vẫn còn trên màn hình.", true);
                return false;
            }
            savedData = true;
        }

        if (IsReportVisible && ReportHasChanges())
        {
            var reportResult = await _workLogService.SaveReportAsync(id,
                new WorkLogReportDraft(WhatWasDone, Result, Learned, Blocker, NextStep));
            if (!reportResult.Succeeded)
            {
                _dialogs.ShowMessage("Chưa lưu được báo cáo", reportResult.Error ?? "Nội dung vẫn còn trên màn hình.", true);
                return false;
            }
            savedData = true;
        }

        if (IsQuickScopeFormVisible && (!string.IsNullOrWhiteSpace(QuickScopeTitle) ||
            !string.IsNullOrWhiteSpace(QuickScopeReason) || QuickScopeStatus != ScopeItemStatus.Backlog))
        {
            var scopeResult = await _scopeItemService.SaveAsync(Session.ProblemId, null,
                new ScopeItemDraft(QuickScopeTitle, QuickScopeReason, QuickScopeStatus));
            if (!scopeResult.Succeeded)
            {
                _dialogs.ShowMessage("Chưa ghi nhận được chủ đề", scopeResult.Error ?? "Nội dung vẫn còn trên màn hình.", true);
                return false;
            }
            savedData = true;
            CancelQuickScope();
        }

        if (!refreshAfterSave)
        {
            if (savedData)
                SessionChanged?.Invoke();
            return true;
        }

        await LoadSessionDetailsAsync(id);
        if (savedData)
            SessionChanged?.Invoke();
        return !HasUnsavedChanges;
    }

    public void DiscardPendingChanges()
    {
        if (Session is null)
            return;
        TaskTitle = Session.TaskTitle;
        WhatWasDone = Session.WhatWasDone ?? string.Empty;
        Result = Session.Result ?? string.Empty;
        Learned = Session.Learned ?? string.Empty;
        Blocker = Session.Blocker ?? string.Empty;
        NextStep = Session.NextStep ?? string.Empty;
        EvidenceDescription = string.Empty;
        CancelQuickScope();
    }

    private async Task AddEvidenceAsync()
    {
        if (Session is null)
            return;
        var sourcePath = _evidenceFilePicker.ChooseImageFile();
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;
        var result = await _evidenceService.AddAsync(Session.ProblemId, Session.Id, sourcePath, EvidenceDescription);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa thêm được minh chứng", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        EvidenceDescription = string.Empty;
        await LoadEvidenceAsync(Session);
    }

    private void OpenSelectedEvidence()
    {
        if (SelectedEvidence is null)
            return;
        try
        {
            _evidenceOpener.Open(SelectedEvidence.AbsolutePath);
        }
        catch (Exception)
        {
            _dialogs.ShowMessage("Không mở được ảnh", "Tệp ảnh có thể đã bị xóa hoặc định dạng chưa được hỗ trợ.", true);
        }
    }

    private async Task DeleteSelectedEvidenceAsync()
    {
        if (SelectedEvidence is null ||
            !_dialogs.Confirm("Xóa minh chứng", "Bạn có muốn xóa ảnh này khỏi PathForge không?"))
            return;
        var result = await _evidenceService.DeleteAsync(SelectedEvidence.Id);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa xóa được ảnh", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        if (Session is not null)
            await LoadEvidenceAsync(Session);
        if (!string.IsNullOrWhiteSpace(result.Error))
            _dialogs.ShowMessage("Ảnh đã xóa, còn tệp cần dọn", result.Error, true);
    }

    private async Task RunSessionActionAsync(
        Func<int, CancellationToken, Task<WorkLogOperationResult>> action, string title)
    {
        if (Session is null)
            return;
        var result = await action(Session.Id, CancellationToken.None);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage(title, result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync();
        SessionChanged?.Invoke();
    }

    private void UpdateElapsedText()
    {
        if (Session is null)
        {
            ElapsedText = "00:00";
            return;
        }
        var total = Math.Max(0, Session.ElapsedSeconds(DateTime.UtcNow));
        var time = TimeSpan.FromSeconds(total);
        ElapsedText = time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"mm\:ss");
    }

    private void SyncTimer()
    {
        if (IsRunning)
        {
            if (!_timer.IsEnabled)
                _timer.Start();
        }
        else if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    private void NotifyCommands()
    {
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        EndCommand.NotifyCanExecuteChanged();
        AbandonCommand.NotifyCanExecuteChanged();
        SaveReportCommand.NotifyCanExecuteChanged();
        SaveTaskTitleCommand.NotifyCanExecuteChanged();
        ShowQuickScopeCommand.NotifyCanExecuteChanged();
        SaveQuickScopeCommand.NotifyCanExecuteChanged();
        CancelQuickScopeCommand.NotifyCanExecuteChanged();
        AddEvidenceCommand.NotifyCanExecuteChanged();
    }

    private bool ReportHasChanges() => Session is not null &&
        (!Same(WhatWasDone, Session.WhatWasDone) || !Same(Result, Session.Result) ||
         !Same(Learned, Session.Learned) || !Same(Blocker, Session.Blocker) || !Same(NextStep, Session.NextStep));

    private static bool Same(string? left, string? right) =>
        string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);

    public void Dispose() => _timer.Stop();

    private bool SetPendingProperty<T>(ref T field, T value)
    {
        if (!SetProperty(ref field, value))
            return false;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        return true;
    }
}
