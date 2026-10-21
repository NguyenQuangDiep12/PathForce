using System.Collections.ObjectModel;
using PathForge.Application.History;
using PathForge.Desktop.Services;

namespace PathForge.Desktop.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IEvidenceOpener _evidenceOpener;
    private readonly IUserDialogService _dialogs;
    private string _keyword = string.Empty;
    private string _message = string.Empty;
    private HistorySearchResult? _selectedResult;

    public HistoryViewModel(IHistoryService historyService, IEvidenceOpener evidenceOpener, IUserDialogService dialogs)
    {
        _historyService = historyService;
        _evidenceOpener = evidenceOpener;
        _dialogs = dialogs;
        _message = "Nhập từ khóa để tìm trong bài toán, mục phạm vi, báo cáo và minh chứng.";
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        OpenCommand = new RelayCommand(OpenSelected, () => SelectedResult is not null);
    }

    public event Action<HistorySearchResult>? OpenResultRequested;
    public ObservableCollection<HistorySearchResult> Results { get; } = new();
    public AsyncRelayCommand SearchCommand { get; }
    public RelayCommand OpenCommand { get; }
    public string Keyword { get => _keyword; set => SetProperty(ref _keyword, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public HistorySearchResult? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (SetProperty(ref _selectedResult, value))
                OpenCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            Results.Clear();
            Message = "Nhập từ khóa để tìm trong bài toán, mục phạm vi, báo cáo và minh chứng.";
            return;
        }

        var results = await _historyService.SearchAsync(Keyword);
        Results.Clear();
        foreach (var result in results)
            Results.Add(result);
        SelectedResult = Results.FirstOrDefault();
        Message = Results.Count == 0 ? "Không tìm thấy kết quả phù hợp." : $"Tìm thấy {Results.Count} kết quả.";
    }

    private void OpenSelected()
    {
        if (SelectedResult is null)
            return;
        if (SelectedResult.Kind == HistoryResultKind.Evidence && !string.IsNullOrWhiteSpace(SelectedResult.AbsolutePath))
        {
            try
            {
                _evidenceOpener.Open(SelectedResult.AbsolutePath);
            }
            catch (Exception)
            {
                _dialogs.ShowMessage("Không mở được ảnh", "Tệp ảnh có thể đã bị xóa hoặc ứng dụng trên máy không hỗ trợ định dạng này.", true);
            }
            return;
        }
        OpenResultRequested?.Invoke(SelectedResult);
    }
}
