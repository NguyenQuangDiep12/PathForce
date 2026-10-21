using System.Collections.ObjectModel;
using PathForge.Application.Evidence;
using PathForge.Application.Problems;
using PathForge.Application.ScopeItems;
using PathForge.Application.WorkLogs;
using PathForge.Desktop.Services;
using PathForge.Domain.Enums;

namespace PathForge.Desktop.ViewModels;

public sealed class ProblemsViewModel : ObservableObject
{
    private readonly IProblemService _problemService;
    private readonly IScopeItemService _scopeItemService;
    private readonly IEvidenceService _evidenceService;
    private readonly IUserDialogService _dialogs;
    private readonly IEvidenceFilePicker _evidenceFilePicker;
    private readonly IEvidenceOpener _evidenceOpener;
    private readonly IWorkLogService _workLogService;
    private string _keyword = string.Empty;
    private ProblemListItem? _selectedProblem;
    private int? _selectedProblemId;
    private ProblemStatus _status = ProblemStatus.NotStarted;
    private string _title = string.Empty;
    private string _goal = string.Empty;
    private string _description = string.Empty;
    private string _successCriteria = string.Empty;
    private string _nextAction = string.Empty;
    private bool _requiresEvidence;
    private string _validationMessage = string.Empty;
    private bool _isNew = true;
    private ScopeItemDetails? _selectedScopeItem;
    private int? _selectedScopeItemId;
    private string _scopeTitle = string.Empty;
    private string _scopeReason = string.Empty;
    private string _scopeMessage = string.Empty;
    private EvidenceDetails? _selectedEvidence;
    private string _evidenceDescription = string.Empty;
    private string _evidenceMessage = string.Empty;
    private WorkLogDetails? _selectedWorkLog;

    public ProblemsViewModel(
        IProblemService problemService,
        IScopeItemService scopeItemService,
        IEvidenceService evidenceService,
        IWorkLogService workLogService,
        IUserDialogService dialogs,
        IEvidenceFilePicker evidenceFilePicker,
        IEvidenceOpener evidenceOpener)
    {
        _problemService = problemService;
        _scopeItemService = scopeItemService;
        _evidenceService = evidenceService;
        _workLogService = workLogService;
        _dialogs = dialogs;
        _evidenceFilePicker = evidenceFilePicker;
        _evidenceOpener = evidenceOpener;
        SearchCommand = new AsyncRelayCommand(() => RefreshAsync());
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        NewProblemCommand = new RelayCommand(BeginNewProblem);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => _selectedProblemId.HasValue);
        MoveToInProgressCommand = new AsyncRelayCommand(() => ChangeStatusAsync(ProblemStatus.InProgress), CanMoveToInProgress);
        MoveToBlockedCommand = new AsyncRelayCommand(() => ChangeStatusAsync(ProblemStatus.Blocked), CanMoveToBlocked);
        ResolveCommand = new AsyncRelayCommand(() => ChangeStatusAsync(ProblemStatus.Resolved), CanResolve);
        NewScopeItemCommand = new RelayCommand(BeginNewScopeItem, () => IsExisting);
        SaveScopeItemCommand = new AsyncRelayCommand(SaveScopeItemAsync, () => IsExisting);
        DeleteScopeItemCommand = new AsyncRelayCommand(DeleteScopeItemAsync, () => _selectedScopeItemId.HasValue);
        ScopeInScopeCommand = new AsyncRelayCommand(() => ChangeScopeStatusAsync(ScopeItemStatus.InScope), () => _selectedScopeItemId.HasValue);
        ScopeBacklogCommand = new AsyncRelayCommand(() => ChangeScopeStatusAsync(ScopeItemStatus.Backlog), () => _selectedScopeItemId.HasValue);
        ScopeDoneCommand = new AsyncRelayCommand(() => ChangeScopeStatusAsync(ScopeItemStatus.Done), () => _selectedScopeItemId.HasValue);
        ScopeExcludedCommand = new AsyncRelayCommand(() => ChangeScopeStatusAsync(ScopeItemStatus.Excluded), () => _selectedScopeItemId.HasValue);
        AddEvidenceCommand = new AsyncRelayCommand(AddEvidenceAsync, () => IsExisting);
        OpenEvidenceCommand = new RelayCommand(OpenEvidence, () => SelectedEvidence is not null);
        DeleteEvidenceCommand = new AsyncRelayCommand(DeleteEvidenceAsync, () => SelectedEvidence is not null);
        OpenWorkLogCommand = new RelayCommand(() =>
        {
            if (SelectedWorkLog is not null)
                OpenWorkLogRequested?.Invoke(SelectedWorkLog.Id);
        }, () => SelectedWorkLog is not null);
    }

    public event Action? ProblemChanged;
    public event Action<int>? OpenWorkLogRequested;
    public ObservableCollection<ProblemListItem> Problems { get; } = new();
    public ObservableCollection<ScopeItemDetails> ScopeItems { get; } = new();
    public ObservableCollection<EvidenceDetails> EvidenceItems { get; } = new();
    public ObservableCollection<WorkLogDetails> RecentWorkLogs { get; } = new();
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand NewProblemCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand MoveToInProgressCommand { get; }
    public AsyncRelayCommand MoveToBlockedCommand { get; }
    public AsyncRelayCommand ResolveCommand { get; }
    public RelayCommand NewScopeItemCommand { get; }
    public AsyncRelayCommand SaveScopeItemCommand { get; }
    public AsyncRelayCommand DeleteScopeItemCommand { get; }
    public AsyncRelayCommand ScopeInScopeCommand { get; }
    public AsyncRelayCommand ScopeBacklogCommand { get; }
    public AsyncRelayCommand ScopeDoneCommand { get; }
    public AsyncRelayCommand ScopeExcludedCommand { get; }
    public AsyncRelayCommand AddEvidenceCommand { get; }
    public RelayCommand OpenEvidenceCommand { get; }
    public AsyncRelayCommand DeleteEvidenceCommand { get; }
    public RelayCommand OpenWorkLogCommand { get; }

    public string Keyword
    {
        get => _keyword;
        set
        {
            if (SetProperty(ref _keyword, value))
                OnPropertyChanged(nameof(EmptyListMessage));
        }
    }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Goal { get => _goal; set => SetProperty(ref _goal, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string SuccessCriteria { get => _successCriteria; set => SetProperty(ref _successCriteria, value); }
    public string NextAction { get => _nextAction; set => SetProperty(ref _nextAction, value); }
    public bool RequiresEvidence { get => _requiresEvidence; set => SetProperty(ref _requiresEvidence, value); }
    public string ValidationMessage { get => _validationMessage; private set => SetProperty(ref _validationMessage, value); }
    public string ScopeTitle { get => _scopeTitle; set => SetProperty(ref _scopeTitle, value); }
    public string ScopeReason { get => _scopeReason; set => SetProperty(ref _scopeReason, value); }
    public string ScopeMessage { get => _scopeMessage; private set => SetProperty(ref _scopeMessage, value); }
    public string EvidenceDescription { get => _evidenceDescription; set => SetProperty(ref _evidenceDescription, value); }
    public string EvidenceMessage { get => _evidenceMessage; private set => SetProperty(ref _evidenceMessage, value); }
    public bool IsNew { get => _isNew; private set => SetProperty(ref _isNew, value); }
    public bool IsExisting => !IsNew && _selectedProblemId.HasValue;
    public bool HasProblems => Problems.Count > 0;
    public string EmptyListMessage => string.IsNullOrWhiteSpace(Keyword)
        ? "Chưa có bài toán nào."
        : "Không tìm thấy bài toán phù hợp.";
    public string ScopeStatusText => SelectedScopeItem?.Status switch
    {
        ScopeItemStatus.InScope => "Trong phạm vi",
        ScopeItemStatus.Backlog => "Để sau",
        ScopeItemStatus.Done => "Đã xử lý",
        ScopeItemStatus.Excluded => "Đã loại khỏi phạm vi",
        _ => string.Empty
    };
    public string StatusText => _status switch
    {
        ProblemStatus.NotStarted => "Chưa bắt đầu",
        ProblemStatus.InProgress => "Đang thực hiện",
        ProblemStatus.Blocked => "Bị chặn",
        ProblemStatus.Resolved => "Đã hoàn thành",
        _ => string.Empty
    };

    public ProblemListItem? SelectedProblem
    {
        get => _selectedProblem;
        set
        {
            if (SetProperty(ref _selectedProblem, value) && value is not null)
                _ = LoadProblemAsync(value.Id);
        }
    }

    public ScopeItemDetails? SelectedScopeItem
    {
        get => _selectedScopeItem;
        set
        {
            if (!SetProperty(ref _selectedScopeItem, value))
                return;
            if (value is null)
                return;
            _selectedScopeItemId = value.Id;
            ScopeTitle = value.Title;
            ScopeReason = value.Reason ?? string.Empty;
            ScopeMessage = string.Empty;
            OnPropertyChanged(nameof(ScopeStatusText));
            NotifyScopeCommands();
        }
    }

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

    public WorkLogDetails? SelectedWorkLog
    {
        get => _selectedWorkLog;
        set
        {
            if (SetProperty(ref _selectedWorkLog, value))
                OpenWorkLogCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task RefreshAsync(int? selectId = null)
    {
        var items = await _problemService.SearchAsync(Keyword);
        Problems.Clear();
        foreach (var item in items)
            Problems.Add(item);
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(EmptyListMessage));

        if (selectId.HasValue)
        {
            var selected = Problems.FirstOrDefault(problem => problem.Id == selectId.Value);
            if (selected is not null)
                SelectedProblem = selected;
        }
        else if (_selectedProblemId.HasValue)
        {
            var selected = Problems.FirstOrDefault(problem => problem.Id == _selectedProblemId.Value);
            if (selected is not null)
                SelectedProblem = selected;
        }
        else if (Problems.Count > 0 && IsNew)
            SelectedProblem = Problems[0];
    }

    public void FocusNewProblem() => BeginNewProblem();

    public async Task OpenProblemAsync(int problemId, int? scopeItemId = null)
    {
        Keyword = string.Empty;
        await RefreshAsync(problemId);
        SelectedProblem = Problems.FirstOrDefault(problem => problem.Id == problemId);
        await LoadProblemAsync(problemId);
        if (scopeItemId.HasValue)
            SelectedScopeItem = ScopeItems.FirstOrDefault(item => item.Id == scopeItemId.Value);
    }

    private void BeginNewProblem()
    {
        SelectedProblem = null;
        _selectedProblemId = null;
        IsNew = true;
        _status = ProblemStatus.NotStarted;
        Title = string.Empty;
        Goal = string.Empty;
        Description = string.Empty;
        SuccessCriteria = string.Empty;
        NextAction = string.Empty;
        RequiresEvidence = false;
        ScopeItems.Clear();
        EvidenceItems.Clear();
        RecentWorkLogs.Clear();
        SelectedWorkLog = null;
        BeginNewScopeItem();
        SelectedEvidence = null;
        EvidenceDescription = string.Empty;
        EvidenceMessage = string.Empty;
        ValidationMessage = string.Empty;
        NotifyStatusChanged();
    }

    private async Task LoadProblemAsync(int id)
    {
        var problem = await _problemService.GetAsync(id);
        if (problem is null)
            return;
        _selectedProblemId = problem.Id;
        IsNew = false;
        _status = problem.Status;
        Title = problem.Title;
        Goal = problem.Goal;
        Description = problem.Description ?? string.Empty;
        SuccessCriteria = problem.SuccessCriteria ?? string.Empty;
        NextAction = problem.NextAction ?? string.Empty;
        RequiresEvidence = problem.RequiresEvidence;
        ValidationMessage = string.Empty;
        NotifyStatusChanged();
        await LoadScopeItemsAsync(problem.Id);
        await LoadEvidenceAsync(problem.Id);
        await LoadWorkLogsAsync(problem.Id);
    }

    private async Task SaveAsync()
    {
        ValidationMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationMessage = "Vui lòng nhập tên bài toán.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Goal))
        {
            ValidationMessage = "Vui lòng nhập mục tiêu cần đạt.";
            return;
        }

        var result = await _problemService.SaveAsync(_selectedProblemId, new ProblemDraft
        {
            Title = Title,
            Goal = Goal,
            Description = Description,
            SuccessCriteria = SuccessCriteria,
            NextAction = NextAction,
            RequiresEvidence = RequiresEvidence
        });

        if (!result.Succeeded)
        {
            ValidationMessage = result.Error ?? "Không thể lưu bài toán. Vui lòng thử lại.";
            return;
        }

        IsNew = false;
        _selectedProblemId = result.ProblemId;
        Keyword = string.Empty;
        await RefreshAsync(result.ProblemId);
        ProblemChanged?.Invoke();
    }

    private async Task ChangeStatusAsync(ProblemStatus target)
    {
        if (!_selectedProblemId.HasValue)
            return;

        if (target == ProblemStatus.Resolved)
        {
            var criteria = string.IsNullOrWhiteSpace(SuccessCriteria)
                ? "Chưa có tiêu chí thành công được ghi lại."
                : "Tiêu chí thành công cần đối chiếu:\n" + SuccessCriteria;
            if (!_dialogs.Confirm("Xác nhận hoàn thành", criteria + "\n\nBạn xác nhận mục tiêu bài toán đã đạt?") )
                return;
        }

        var result = await _problemService.ChangeStatusAsync(_selectedProblemId.Value, target);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa cập nhật được trạng thái", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        await RefreshAsync(_selectedProblemId.Value);
        ProblemChanged?.Invoke();
    }

    private async Task DeleteAsync()
    {
        if (!_selectedProblemId.HasValue)
            return;
        if (!_dialogs.Confirm("Xóa bài toán", "Bài toán cùng các mục nhỏ, phiên làm việc và minh chứng liên quan sẽ bị xóa. Bạn có muốn tiếp tục?"))
            return;

        var result = await _problemService.DeleteAsync(_selectedProblemId.Value);
        if (!result.Succeeded)
        {
            _dialogs.ShowMessage("Chưa xóa được bài toán", result.Error ?? "Vui lòng thử lại.", true);
            return;
        }
        BeginNewProblem();
        await RefreshAsync();
        ProblemChanged?.Invoke();
    }

    private bool CanMoveToInProgress() => _selectedProblemId.HasValue && _status != ProblemStatus.InProgress;
    private bool CanMoveToBlocked() => _selectedProblemId.HasValue && (_status == ProblemStatus.NotStarted || _status == ProblemStatus.InProgress);
    private bool CanResolve() => _selectedProblemId.HasValue && (_status == ProblemStatus.InProgress || _status == ProblemStatus.Blocked);

    private async Task LoadScopeItemsAsync(int problemId)
    {
        var items = await _scopeItemService.GetByProblemAsync(problemId);
        ScopeItems.Clear();
        foreach (var item in items)
            ScopeItems.Add(item);
        SelectedScopeItem = null;
        _selectedScopeItemId = null;
        ScopeTitle = string.Empty;
        ScopeReason = string.Empty;
        ScopeMessage = string.Empty;
        NotifyScopeCommands();
    }

    private async Task LoadEvidenceAsync(int problemId)
    {
        var items = await _evidenceService.GetByProblemAsync(problemId);
        EvidenceItems.Clear();
        foreach (var item in items)
            EvidenceItems.Add(item);
        SelectedEvidence = null;
        EvidenceMessage = string.Empty;
    }

    private async Task LoadWorkLogsAsync(int problemId)
    {
        var items = await _workLogService.GetByProblemAsync(problemId);
        RecentWorkLogs.Clear();
        foreach (var item in items.Take(5))
            RecentWorkLogs.Add(item);
        SelectedWorkLog = RecentWorkLogs.FirstOrDefault();
    }

    private async Task AddEvidenceAsync()
    {
        if (!_selectedProblemId.HasValue)
            return;
        var path = _evidenceFilePicker.ChooseImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;
        var result = await _evidenceService.AddAsync(_selectedProblemId.Value, null, path, EvidenceDescription);
        if (!result.Succeeded)
        {
            EvidenceMessage = result.Error ?? "Không thể thêm ảnh minh chứng.";
            return;
        }
        EvidenceDescription = string.Empty;
        await LoadEvidenceAsync(_selectedProblemId.Value);
    }

    private void OpenEvidence()
    {
        if (SelectedEvidence is null)
            return;
        try
        {
            _evidenceOpener.Open(SelectedEvidence.AbsolutePath);
        }
        catch (Exception)
        {
            EvidenceMessage = "Không thể mở ảnh bằng ứng dụng trên máy này.";
        }
    }

    private async Task DeleteEvidenceAsync()
    {
        if (SelectedEvidence is null ||
            !_dialogs.Confirm("Xóa minh chứng", "Bạn có muốn xóa ảnh này khỏi PathForge không?"))
            return;
        var result = await _evidenceService.DeleteAsync(SelectedEvidence.Id);
        if (!result.Succeeded)
        {
            EvidenceMessage = result.Error ?? "Không thể xóa ảnh minh chứng.";
            return;
        }
        if (_selectedProblemId.HasValue)
            await LoadEvidenceAsync(_selectedProblemId.Value);
    }

    private void BeginNewScopeItem()
    {
        SelectedScopeItem = null;
        _selectedScopeItemId = null;
        ScopeTitle = string.Empty;
        ScopeReason = string.Empty;
        ScopeMessage = string.Empty;
        OnPropertyChanged(nameof(ScopeStatusText));
        NotifyScopeCommands();
    }

    private async Task SaveScopeItemAsync()
    {
        if (!_selectedProblemId.HasValue)
            return;
        var result = await _scopeItemService.SaveAsync(_selectedProblemId.Value, _selectedScopeItemId,
            new ScopeItemDraft(ScopeTitle, ScopeReason));
        if (!result.Succeeded)
        {
            ScopeMessage = result.Error ?? "Không thể lưu mục phạm vi.";
            return;
        }
        await LoadScopeItemsAsync(_selectedProblemId.Value);
    }

    private async Task ChangeScopeStatusAsync(ScopeItemStatus status)
    {
        if (!_selectedScopeItemId.HasValue)
            return;
        var result = await _scopeItemService.ChangeStatusAsync(_selectedScopeItemId.Value, status);
        if (!result.Succeeded)
        {
            ScopeMessage = result.Error ?? "Không thể đổi trạng thái mục phạm vi.";
            return;
        }
        var problemId = _selectedProblemId;
        if (problemId.HasValue)
            await LoadScopeItemsAsync(problemId.Value);
    }

    private async Task DeleteScopeItemAsync()
    {
        if (!_selectedScopeItemId.HasValue ||
            !_dialogs.Confirm("Xóa mục phạm vi", "Bạn có muốn xóa mục nhỏ này không?"))
            return;
        var result = await _scopeItemService.DeleteAsync(_selectedScopeItemId.Value);
        if (!result.Succeeded)
        {
            ScopeMessage = result.Error ?? "Không thể xóa mục phạm vi.";
            return;
        }
        if (_selectedProblemId.HasValue)
            await LoadScopeItemsAsync(_selectedProblemId.Value);
    }

    private void NotifyScopeCommands()
    {
        OnPropertyChanged(nameof(ScopeStatusText));
        NewScopeItemCommand.NotifyCanExecuteChanged();
        SaveScopeItemCommand.NotifyCanExecuteChanged();
        DeleteScopeItemCommand.NotifyCanExecuteChanged();
        ScopeInScopeCommand.NotifyCanExecuteChanged();
        ScopeBacklogCommand.NotifyCanExecuteChanged();
        ScopeDoneCommand.NotifyCanExecuteChanged();
        ScopeExcludedCommand.NotifyCanExecuteChanged();
        AddEvidenceCommand.NotifyCanExecuteChanged();
    }

    private void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsExisting));
        DeleteCommand.NotifyCanExecuteChanged();
        MoveToInProgressCommand.NotifyCanExecuteChanged();
        MoveToBlockedCommand.NotifyCanExecuteChanged();
        ResolveCommand.NotifyCanExecuteChanged();
        NotifyScopeCommands();
    }
}
