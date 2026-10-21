using System.Collections.ObjectModel;
using PathForge.Application.KnowledgeDocuments;
using PathForge.Desktop.Services;

namespace PathForge.Desktop.ViewModels;

public sealed class KnowledgeDocumentsViewModel : ObservableObject
{
    private readonly IKnowledgeDocumentService _documents;
    private readonly IKnowledgeImageStorage _imageStorage;
    private readonly IUserDialogService _dialogs;
    private string _keyword = string.Empty;
    private string _title = string.Empty;
    private string _status = "Tạo tài liệu mới hoặc chọn tài liệu đã lưu.";
    private string _lastUpdatedText = string.Empty;
    private string _currentContent = string.Empty;
    private bool _isDirty;
    private bool _isSettingDocument;
    private bool _isOpeningDocument;
    private bool _suppressSelection;
    private int? _currentDocumentId;
    private KnowledgeDocumentListItem? _selectedDocument;
    private KnowledgeDocumentListItem? _currentListItem;
    private Func<string>? _captureEditorContent;

    public KnowledgeDocumentsViewModel(
        IKnowledgeDocumentService documents,
        IKnowledgeImageStorage imageStorage,
        IUserDialogService dialogs)
    {
        _documents = documents;
        _imageStorage = imageStorage;
        _dialogs = dialogs;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        NewDocumentCommand = new AsyncRelayCommand(CreateNewAsync);
        SaveCommand = new AsyncRelayCommand(async () => await SaveCurrentAsync());
        CloseDocumentCommand = new AsyncRelayCommand(CloseDocumentAsync);
        _ = SearchAsync();
    }

    public ObservableCollection<KnowledgeDocumentListItem> Documents { get; } = new();
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand NewDocumentCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand CloseDocumentCommand { get; }

    public string Keyword { get => _keyword; set => SetProperty(ref _keyword, value); }
    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value) && !_isSettingDocument)
                MarkChanged();
        }
    }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string LastUpdatedText { get => _lastUpdatedText; private set => SetProperty(ref _lastUpdatedText, value); }
    public string CurrentContent { get => _currentContent; private set => SetProperty(ref _currentContent, value); }
    public bool IsDirty { get => _isDirty; private set => SetProperty(ref _isDirty, value); }
    public bool HasDocuments => Documents.Count > 0;

    public KnowledgeDocumentListItem? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (!SetProperty(ref _selectedDocument, value) || _suppressSelection || value is null)
                return;
            _ = OpenDocumentAsync(value);
        }
    }

    public void RegisterEditorSnapshotProvider(Func<string> captureEditorContent) =>
        _captureEditorContent = captureEditorContent;

    public void MarkEditorChanged()
    {
        if (!_isSettingDocument)
            MarkChanged();
    }

    public async Task SearchAsync()
    {
        try
        {
            var results = await _documents.SearchAsync(Keyword);
            Documents.Clear();
            foreach (var document in results)
                Documents.Add(document);
            OnPropertyChanged(nameof(HasDocuments));
            if (_currentDocumentId.HasValue)
            {
                var refreshedCurrent = Documents.FirstOrDefault(document => document.Id == _currentDocumentId.Value);
                if (refreshedCurrent is not null)
                {
                    _currentListItem = refreshedCurrent;
                    RestoreSelectedDocument(refreshedCurrent);
                }
            }
        }
        catch (Exception exception)
        {
            _dialogs.ShowMessage("Không tìm được tài liệu", "Không thể tải danh sách tài liệu. " + exception.Message, true);
        }
    }

    public async Task<bool> SaveCurrentAsync(bool refreshAfterSave = true)
    {
        try
        {
            var content = CaptureEditorContent();
            var result = await _documents.SaveAsync(_currentDocumentId, Title, content);
            if (!result.Succeeded || !result.Id.HasValue)
            {
                Status = "Lưu thất bại. " + (result.Error ?? "Không lưu được tài liệu.");
                _dialogs.ShowMessage("Chưa lưu được tài liệu", Status, true);
                return false;
            }

            _currentDocumentId = result.Id.Value;
            CurrentContent = content;
            IsDirty = false;
            var now = DateTime.Now;
            LastUpdatedText = $"Cập nhật lúc {now:dd/MM/yyyy HH:mm}";
            Status = "Đã lưu tài liệu.";
            if (refreshAfterSave)
                await RefreshDocumentsAndSelectCurrentAsync();
            return true;
        }
        catch (Exception exception)
        {
            Status = "Lưu thất bại. Nội dung đang chỉnh sửa vẫn được giữ lại.";
            _dialogs.ShowMessage("Không lưu được tài liệu", Status + "\n\n" + exception.Message, true);
            return false;
        }
    }

    public async Task<bool> CanCloseAsync(bool refreshAfterSave = true) =>
        await PrepareToLeaveCurrentDocumentAsync(refreshAfterSave);

    public async Task<StoredKnowledgeImage> StoreImageAsync(string sourcePath) =>
        await _imageStorage.StoreAsync(sourcePath);

    private async Task CreateNewAsync()
    {
        if (!await PrepareToLeaveCurrentDocumentAsync())
            return;

        SetCurrentDocument(null, null, string.Empty, string.Empty, DateTime.UtcNow, DateTime.UtcNow);
        Status = "Tài liệu mới. Nhập tên tài liệu rồi lưu.";
        LastUpdatedText = string.Empty;
    }

    private async Task CloseDocumentAsync()
    {
        if (!await PrepareToLeaveCurrentDocumentAsync())
            return;

        SetCurrentDocument(null, null, string.Empty, string.Empty, DateTime.UtcNow, DateTime.UtcNow);
        Status = "Đã đóng tài liệu.";
        LastUpdatedText = string.Empty;
    }

    private async Task OpenDocumentAsync(KnowledgeDocumentListItem requested)
    {
        if (_isOpeningDocument)
            return;
        if (_currentDocumentId == requested.Id)
        {
            _currentListItem = requested;
            return;
        }

        _isOpeningDocument = true;
        try
        {
            if (!await PrepareToLeaveCurrentDocumentAsync())
            {
                RestoreSelectedDocument(_currentListItem);
                return;
            }

            var document = await _documents.GetAsync(requested.Id);
            if (document is null)
            {
                _dialogs.ShowMessage("Không tìm thấy tài liệu", "Tài liệu có thể đã bị xóa hoặc database đã thay đổi.", true);
                await SearchAsync();
                RestoreSelectedDocument(_currentListItem);
                return;
            }

            var listItem = new KnowledgeDocumentListItem(document.Id, document.Title, document.CreatedAt, document.UpdatedAt);
            SetCurrentDocument(document.Id, listItem, document.Title, document.Content, document.CreatedAt, document.UpdatedAt);
        }
        catch (Exception exception)
        {
            _dialogs.ShowMessage("Không mở được tài liệu", "Nội dung tài liệu không thể tải. " + exception.Message, true);
            RestoreSelectedDocument(_currentListItem);
        }
        finally
        {
            _isOpeningDocument = false;
        }
    }

    private async Task<bool> PrepareToLeaveCurrentDocumentAsync(bool refreshAfterSave = true)
    {
        if (!IsDirty)
            return true;

        var choice = _dialogs.AskToSaveChanges("Tài liệu kiến thức");
        if (choice == UnsavedChangesChoice.Cancel)
            return false;
        if (choice == UnsavedChangesChoice.Discard)
            return true;
        return await SaveCurrentAsync(refreshAfterSave);
    }

    private string CaptureEditorContent() => _captureEditorContent?.Invoke() ?? CurrentContent;

    private async Task RefreshDocumentsAndSelectCurrentAsync()
    {
        var results = await _documents.SearchAsync(Keyword);
        Documents.Clear();
        foreach (var document in results)
            Documents.Add(document);
        OnPropertyChanged(nameof(HasDocuments));

        _currentListItem = _currentDocumentId.HasValue
            ? Documents.FirstOrDefault(document => document.Id == _currentDocumentId.Value)
            : null;
        RestoreSelectedDocument(_currentListItem);
    }

    private void SetCurrentDocument(
        int? id,
        KnowledgeDocumentListItem? listItem,
        string title,
        string content,
        DateTime createdAt,
        DateTime updatedAt)
    {
        _isSettingDocument = true;
        try
        {
            _currentDocumentId = id;
            _currentListItem = listItem;
            Title = title;
            CurrentContent = content;
            IsDirty = false;
            LastUpdatedText = id.HasValue
                ? $"Tạo {createdAt.ToLocalTime():dd/MM/yyyy HH:mm} · cập nhật {updatedAt.ToLocalTime():dd/MM/yyyy HH:mm}"
                : string.Empty;
            RestoreSelectedDocument(listItem);
        }
        finally
        {
            _isSettingDocument = false;
        }
    }

    private void RestoreSelectedDocument(KnowledgeDocumentListItem? item)
    {
        _suppressSelection = true;
        try
        {
            SelectedDocument = item;
        }
        finally
        {
            _suppressSelection = false;
        }
    }

    private void MarkChanged()
    {
        IsDirty = true;
        Status = "Có thay đổi chưa lưu.";
    }
}
