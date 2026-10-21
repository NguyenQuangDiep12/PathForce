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
    private string _analysisWhat = string.Empty;
    private string _analysisWhy = string.Empty;
    private string _analysisWho = string.Empty;
    private string _analysisWhere = string.Empty;
    private string _analysisWhen = string.Empty;
    private string _analysisHow = string.Empty;
    private string _successCriteria = string.Empty;
    private string _nextAction = string.Empty;
    private bool _requiresEvidence;
    private string _validationMessage = string.Empty;
    private bool _isNew = true;
    private bool _isExplicitlyCreatingNewProblem;
    private bool _isRefreshingList;
    private int _selectionGeneration;
    private ProblemFormState _savedFormState = new(string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false);
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
        NewProblemCommand = new RelayCommand(() => _ = RequestNewProblemAsync());
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
    public string Title { get => _title; set => SetFormProperty(ref _title, value); }
    public string Goal { get => _goal; set => SetFormProperty(ref _goal, value); }
    public string Description { get => _description; set => SetFormProperty(ref _description, value); }
    public string AnalysisWhat { get => _analysisWhat; set => SetFormProperty(ref _analysisWhat, value); }
    public string AnalysisWhy { get => _analysisWhy; set => SetFormProperty(ref _analysisWhy, value); }
    public string AnalysisWho { get => _analysisWho; set => SetFormProperty(ref _analysisWho, value); }
    public string AnalysisWhere { get => _analysisWhere; set => SetFormProperty(ref _analysisWhere, value); }
    public string AnalysisWhen { get => _analysisWhen; set => SetFormProperty(ref _analysisWhen, value); }
    public string AnalysisHow { get => _analysisHow; set => SetFormProperty(ref _analysisHow, value); }
    public string SuccessCriteria { get => _successCriteria; set => SetFormProperty(ref _successCriteria, value); }
    public string NextAction { get => _nextAction; set => SetFormProperty(ref _nextAction, value); }
    public bool RequiresEvidence { get => _requiresEvidence; set => SetFormProperty(ref _requiresEvidence, value); }
    public string ValidationMessage { get => _validationMessage; private set => SetProperty(ref _validationMessage, value); }
    public string ScopeTitle { get => _scopeTitle; set => SetProperty(ref _scopeTitle, value); }
    public string ScopeReason { get => _scopeReason; set => SetProperty(ref _scopeReason, value); }
    public string ScopeMessage { get => _scopeMessage; private set => SetProperty(ref _scopeMessage, value); }
    public string EvidenceDescription { get => _evidenceDescription; set => SetProperty(ref _evidenceDescription, value); }
    public string EvidenceMessage { get => _evidenceMessage; private set => SetProperty(ref _evidenceMessage, value); }
    public bool IsNew { get => _isNew; private set => SetProperty(ref _isNew, value); }
    public bool IsExisting => !IsNew && _selectedProblemId.HasValue;
    public bool HasProblems => Problems.Count > 0;
    public bool HasUnsavedChanges => CaptureFormState() != _savedFormState;
    public bool HasSelectedEvidence => SelectedEvidence is not null;
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
            var previous = _selectedProblem;
            if (!SetProperty(ref _selectedProblem, value) || _isRefreshingList)
                return;
            if (value is null)
            {
                ++_selectionGeneration;
                return;
            }
            if (previous?.Id != value.Id)
                _ = HandleProblemSelectionAsync(previous, value, ++_selectionGeneration);
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
            OnPropertyChanged(nameof(HasSelectedEvidence));
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
        var generation = _selectionGeneration;
        var previousSelectionId = SelectedProblem?.Id;
        var items = await _problemService.SearchAsync(Keyword);
        if (generation != _selectionGeneration)
            return;

        ProblemListItem? toLoad = null;
        _isRefreshingList = true;
        Problems.Clear();
        foreach (var item in items)
            Problems.Add(item);
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(EmptyListMessage));

        if (selectId.HasValue)
        {
            var selected = Problems.FirstOrDefault(problem => problem.Id == selectId.Value);
            if (selected is not null)
            {
                SelectedProblem = selected;
                toLoad = selected;
            }
        }
        else if (_selectedProblemId.HasValue)
        {
            var selected = Problems.FirstOrDefault(problem => problem.Id == _selectedProblemId.Value);
            if (selected is not null)
            {
                SelectedProblem = selected;
                toLoad = selected;
            }
        }
        else if (Problems.Count > 0 && IsNew && !_isExplicitlyCreatingNewProblem)
        {
            SelectedProblem = Problems[0];
            toLoad = Problems[0];
        }
        _isRefreshingList = false;
        if (toLoad is not null && (previousSelectionId != toLoad.Id || !HasUnsavedChanges))
            await LoadProblemAsync(toLoad.Id, generation);
    }

    public void FocusNewProblem() => BeginNewProblem();

    private async Task RequestNewProblemAsync()
    {
        if (HasUnsavedChanges)
        {
            var choice = _dialogs.AskToSaveChanges("Thông tin bài toán");
            if (choice == UnsavedChangesChoice.Cancel)
                return;
            if (choice == UnsavedChangesChoice.Discard)
                DiscardPendingChanges();
            else if (!await SavePendingChangesAsync())
                return;
        }
        BeginNewProblem();
    }

    public async Task OpenProblemAsync(int problemId, int? scopeItemId = null)
    {
        Keyword = string.Empty;
        await RefreshAsync(problemId);
        if (scopeItemId.HasValue)
            SelectedScopeItem = ScopeItems.FirstOrDefault(item => item.Id == scopeItemId.Value);
    }

    private void BeginNewProblem()
    {
        ++_selectionGeneration;
        _isExplicitlyCreatingNewProblem = true;
        SelectedProblem = null;
        _selectedProblemId = null;
        IsNew = true;
        _status = ProblemStatus.NotStarted;
        Title = string.Empty;
        Goal = string.Empty;
        Description = string.Empty;
        AnalysisWhat = string.Empty;
        AnalysisWhy = string.Empty;
        AnalysisWho = string.Empty;
        AnalysisWhere = string.Empty;
        AnalysisWhen = string.Empty;
        AnalysisHow = string.Empty;
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
        _savedFormState = CaptureFormState();
        ValidationMessage = string.Empty;
        NotifyStatusChanged();
    }

    private async Task LoadProblemAsync(int id, int generation)
    {
        var problem = await _problemService.GetAsync(id);
        if (problem is null || generation != _selectionGeneration || SelectedProblem?.Id != id)
            return;
        _selectedProblemId = problem.Id;
        _isExplicitlyCreatingNewProblem = false;
        IsNew = false;
        _status = problem.Status;
        Title = problem.Title;
        Goal = problem.Goal;
        Description = problem.Description ?? string.Empty;
        AnalysisWhat = problem.AnalysisWhat ?? string.Empty;
        AnalysisWhy = problem.AnalysisWhy ?? string.Empty;
        AnalysisWho = problem.AnalysisWho ?? string.Empty;
        AnalysisWhere = problem.AnalysisWhere ?? string.Empty;
        AnalysisWhen = problem.AnalysisWhen ?? string.Empty;
        AnalysisHow = problem.AnalysisHow ?? string.Empty;
        SuccessCriteria = problem.SuccessCriteria ?? string.Empty;
        NextAction = problem.NextAction ?? string.Empty;
        RequiresEvidence = problem.RequiresEvidence;
        _savedFormState = CaptureFormState();
        ValidationMessage = string.Empty;
        NotifyStatusChanged();
        await LoadScopeItemsAsync(problem.Id, generation);
        if (generation != _selectionGeneration)
            return;
        await LoadEvidenceAsync(problem.Id, generation);
        if (generation != _selectionGeneration)
            return;
        await LoadWorkLogsAsync(problem.Id, generation);
    }

    private Task SaveAsync() => SaveAsync(refreshAfterSave: true);

    private async Task SaveAsync(bool refreshAfterSave)
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

        var createsNewProblem = !_selectedProblemId.HasValue;
        var result = await _problemService.SaveAsync(_selectedProblemId, new ProblemDraft
        {
            Title = Title,
            Goal = Goal,
            Description = Description,
            AnalysisWhat = AnalysisWhat,
            AnalysisWhy = AnalysisWhy,
            AnalysisWho = AnalysisWho,
            AnalysisWhere = AnalysisWhere,
            AnalysisWhen = AnalysisWhen,
            AnalysisHow = AnalysisHow,
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
        _savedFormState = CaptureFormState();
        Keyword = string.Empty;
        if (refreshAfterSave)
        {
            await RefreshAsync(result.ProblemId);
            ProblemChanged?.Invoke();
            if (createsNewProblem)
                BeginNewProblem();
        }
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

        var result = await _problemService.ChangeStatusAsync(
            _selectedProblemId.Value, target, userConfirmedResolution: target == ProblemStatus.Resolved);
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
        if (!string.IsNullOrWhiteSpace(result.Warning))
            _dialogs.ShowMessage("Bài toán đã xóa, còn tệp cần dọn", result.Warning, true);
        ProblemChanged?.Invoke();
    }

    public async Task<bool> SavePendingChangesAsync(bool refreshAfterSave = true)
    {
        if (!HasUnsavedChanges)
            return true;
        await SaveAsync(refreshAfterSave);
        return !HasUnsavedChanges;
    }

    public void DiscardPendingChanges()
    {
        ApplyFormState(_savedFormState);
        ValidationMessage = string.Empty;
    }

    private async Task HandleProblemSelectionAsync(ProblemListItem? previous, ProblemListItem next, int generation)
    {
        if (HasUnsavedChanges)
        {
            var choice = _dialogs.AskToSaveChanges("Thông tin bài toán");
            if (choice == UnsavedChangesChoice.Cancel)
            {
                RestoreProblemSelection(previous, generation);
                return;
            }
            if (choice == UnsavedChangesChoice.Discard)
                DiscardPendingChanges();
            else if (!await SavePendingChangesAsync())
            {
                RestoreProblemSelection(previous, generation);
                return;
            }
        }

        if (generation == _selectionGeneration)
            await LoadProblemAsync(next.Id, generation);
    }

    private void RestoreProblemSelection(ProblemListItem? previous, int generation)
    {
        if (generation != _selectionGeneration)
            return;
        _selectedProblem = previous;
        OnPropertyChanged(nameof(SelectedProblem));
    }

    private bool SetFormProperty<T>(ref T field, T value)
    {
        if (!SetProperty(ref field, value))
            return false;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        return true;
    }

    private ProblemFormState CaptureFormState() => new(
        Title, Goal, Description, AnalysisWhat, AnalysisWhy, AnalysisWho, AnalysisWhere,
        AnalysisWhen, AnalysisHow, SuccessCriteria, NextAction, RequiresEvidence);

    private void ApplyFormState(ProblemFormState state)
    {
        Title = state.Title;
        Goal = state.Goal;
        Description = state.Description;
        AnalysisWhat = state.AnalysisWhat;
        AnalysisWhy = state.AnalysisWhy;
        AnalysisWho = state.AnalysisWho;
        AnalysisWhere = state.AnalysisWhere;
        AnalysisWhen = state.AnalysisWhen;
        AnalysisHow = state.AnalysisHow;
        SuccessCriteria = state.SuccessCriteria;
        NextAction = state.NextAction;
        RequiresEvidence = state.RequiresEvidence;
    }

    private sealed record ProblemFormState(
        string Title, string Goal, string Description, string AnalysisWhat, string AnalysisWhy,
        string AnalysisWho, string AnalysisWhere, string AnalysisWhen, string AnalysisHow,
        string SuccessCriteria, string NextAction, bool RequiresEvidence);

    private bool CanMoveToInProgress() => _selectedProblemId.HasValue && _status != ProblemStatus.InProgress;
    private bool CanMoveToBlocked() => _selectedProblemId.HasValue && (_status == ProblemStatus.NotStarted || _status == ProblemStatus.InProgress);
    private bool CanResolve() => _selectedProblemId.HasValue && (_status == ProblemStatus.InProgress || _status == ProblemStatus.Blocked);

    private async Task LoadScopeItemsAsync(int problemId, int? loadGeneration = null)
    {
        var generation = loadGeneration ?? _selectionGeneration;
        var items = await _scopeItemService.GetByProblemAsync(problemId);
        if (generation != _selectionGeneration || _selectedProblemId != problemId)
            return;
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

    private async Task LoadEvidenceAsync(int problemId, int? loadGeneration = null)
    {
        var generation = loadGeneration ?? _selectionGeneration;
        var items = await _evidenceService.GetByProblemAsync(problemId);
        if (generation != _selectionGeneration || _selectedProblemId != problemId)
            return;
        EvidenceItems.Clear();
        foreach (var item in items)
            EvidenceItems.Add(item);
        SelectedEvidence = null;
        EvidenceMessage = string.Empty;
    }

    private async Task LoadWorkLogsAsync(int problemId, int? loadGeneration = null)
    {
        var generation = loadGeneration ?? _selectionGeneration;
        var items = await _workLogService.GetByProblemAsync(problemId);
        if (generation != _selectionGeneration || _selectedProblemId != problemId)
            return;
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
        if (!string.IsNullOrWhiteSpace(result.Error))
            _dialogs.ShowMessage("Ảnh đã xóa, còn tệp cần dọn", result.Error, true);
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
