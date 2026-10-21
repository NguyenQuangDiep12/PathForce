using System.Collections.ObjectModel;
using System.Windows.Threading;
using PathForge.Application.Evidence;
using PathForge.Application.Problems;
using PathForge.Application.ScopeItems;
using PathForge.Application.WorkLogs;
using PathForge.Desktop.Services;
using PathForge.Domain.Enums;

namespace PathForge.Desktop.ViewModels;

public sealed class WorkSessionViewModel : ObservableObject
{
    private readonly IProblemService _problemService;
    private readonly IScopeItemService _scopeItemService;
    private readonly IWorkLogService _workLogService;
    private readonly IEvidenceService _evidenceService;
    private readonly IUserDialogService _dialogs;
    private readonly IEvidenceFilePicker _evidenceFilePicker;
    private readonly IEvidenceOpener _evidenceOpener;
    private readonly DispatcherTimer _timer;
    private ProblemListItem? _selectedProblem;
    private ScopeItemDetails? _selectedScopeItem;
    private WorkLogDetails? _session;
    private string _taskTitle = string.Empty;
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
        AddEvidenceCommand = new AsyncRelayCommand(AddEvidenceAsync, () => Session is not null);
        OpenEvidenceCommand = new RelayCommand(OpenSelectedEvidence, () => SelectedEvidence is not null);
        DeleteEvidenceCommand = new AsyncRelayCommand(DeleteSelectedEvidenceAsync, () => SelectedEvidence is not null);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateElapsedText();
        _timer.Start();
    }

    public ObservableCollection<ProblemListItem> Problems { get; } = new();
    public ObservableCollection<ScopeItemDetails> ScopeItems { get; } = new();
    public ObservableCollection<EvidenceDetails> EvidenceItems { get; } = new();
    public ObservableCollection<string> Durations { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand PauseCommand { get; }
    public AsyncRelayCommand ResumeCommand { get; }
    public AsyncRelayCommand EndCommand { get; }
    public AsyncRelayCommand AbandonCommand { get; }
    public AsyncRelayCommand SaveReportCommand { get; }
    public AsyncRelayCommand AddEvidenceCommand { get; }
    public RelayCommand OpenEvidenceCommand { get; }
    public AsyncRelayCommand DeleteEvidenceCommand { get; }

    public ProblemListItem? SelectedProblem
    {
        get => _selectedProblem;
        set
        {
            if (!SetProperty(ref _selectedProblem, value))
                return;
            SelectedScopeItem = null;
            ScopeItems.Clear();
            if (value is not null)
                _ = LoadScopeItemsAsync(value.Id);
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
            OnPropertyChanged(nameof(SessionStatusText));
            UpdateElapsedText();
            AddEvidenceCommand.NotifyCanExecuteChanged();
            NotifyCommands();
        }
    }

    public string TaskTitle { get => _taskTitle; set => SetProperty(ref _taskTitle, value); }
    public string SelectedDuration { get => _selectedDuration; set => SetProperty(ref _selectedDuration, value); }
    public string ElapsedText { get => _elapsedText; private set => SetProperty(ref _elapsedText, value); }
    public string WhatWasDone { get => _whatWasDone; set => SetProperty(ref _whatWasDone, value); }
    public string Result { get => _result; set => SetProperty(ref _result, value); }
    public string Learned { get => _learned; set => SetProperty(ref _learned, value); }
    public string Blocker { get => _blocker; set => SetProperty(ref _blocker, value); }
    public string NextStep { get => _nextStep; set => SetProperty(ref _nextStep, value); }
    public string EvidenceDescription { get => _evidenceDescription; set => SetProperty(ref _evidenceDescription, value); }
    public bool IsIdle => Session is null;
    public bool IsRunning => Session?.Status == WorkLogStatus.Running;
    public bool IsPaused => Session?.Status == WorkLogStatus.Paused;
    public bool IsAwaitingReport => Session?.Status == WorkLogStatus.AwaitingReport;
    public bool IsReportVisible => Session?.Status == WorkLogStatus.AwaitingReport || Session?.Status == WorkLogStatus.Completed;
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
            OpenEvidenceCommand.NotifyCanExecuteChanged();
            DeleteEvidenceCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task RefreshAsync()
    {
        var problems = await _problemService.SearchAsync(null);
        Problems.Clear();
        foreach (var problem in problems.Where(item => item.Status != ProblemStatus.Resolved))
            Problems.Add(problem);

        var openSession = await _workLogService.GetOpenAsync();
        Session = openSession;
        if (openSession is not null)
        {
            _selectedProblem = Problems.FirstOrDefault(problem => problem.Id == openSession.ProblemId);
            OnPropertyChanged(nameof(SelectedProblem));
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
        SessionChanged?.Invoke();
    }

    public async Task LoadHistoryWorkLogAsync(int workLogId)
    {
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

    private async Task LoadScopeItemsAsync(int problemId)
    {
        var items = await _scopeItemService.GetByProblemAsync(problemId);
        ScopeItems.Clear();
        foreach (var item in items.Where(item => item.Status == ScopeItemStatus.InScope))
            ScopeItems.Add(item);
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
        var result = await _workLogService.StartAsync(SelectedProblem.Id, TaskTitle, duration);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa bắt đầu được phiên", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync();
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
            _dialogs.ShowMessage("Chưa bỏ được phiên", result.Error ?? "Vui lòng thử lại.", true);
        await RefreshAsync();
    }

    private async Task SaveReportAsync()
    {
        if (Session is null)
            return;
        var result = await _workLogService.SaveReportAsync(Session.Id,
            new WorkLogReportDraft(WhatWasDone, Result, Learned, Blocker, NextStep));
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa lưu được báo cáo", result.Error ?? "Nội dung vẫn còn trên màn hình. Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync();
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

    private void NotifyCommands()
    {
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        EndCommand.NotifyCanExecuteChanged();
        AbandonCommand.NotifyCanExecuteChanged();
        SaveReportCommand.NotifyCanExecuteChanged();
        AddEvidenceCommand.NotifyCanExecuteChanged();
    }
}
