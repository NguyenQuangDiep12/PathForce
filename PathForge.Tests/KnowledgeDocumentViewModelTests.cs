using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PathForge.Application.KnowledgeDocuments;
using PathForge.Desktop.Services;
using PathForge.Desktop.ViewModels;
using Xunit;

namespace PathForge.Tests;

public sealed class KnowledgeDocumentViewModelTests
{
    [Fact]
    public async Task FailedSaveKeepsEditorPayloadAndUnsavedState()
    {
        var service = new FailingKnowledgeDocumentService();
        var dialogs = new FakeDialogs();
        var viewModel = new KnowledgeDocumentsViewModel(service, new FakeImageStorage(), dialogs);
        const string draft = "rich-content-package-with-an-image";
        viewModel.RegisterEditorSnapshotProvider(() => draft);
        viewModel.Title = "Tài liệu thử";
        viewModel.MarkEditorChanged();

        var saved = await viewModel.SaveCurrentAsync();

        Assert.False(saved);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(draft, service.LastContent);
        Assert.Contains("Lưu thất bại", viewModel.Status);
        Assert.Equal(1, dialogs.MessageCount);
    }

    [Fact]
    public async Task ClosingDirtyDocumentCanBeCancelled()
    {
        var dialogs = new FakeDialogs { Choice = UnsavedChangesChoice.Cancel };
        var viewModel = new KnowledgeDocumentsViewModel(new FailingKnowledgeDocumentService(), new FakeImageStorage(), dialogs);
        viewModel.RegisterEditorSnapshotProvider(() => "draft");
        viewModel.Title = "Tài liệu chưa lưu";

        var canClose = await viewModel.CanCloseAsync();

        Assert.False(canClose);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(1, dialogs.ConfirmationCount);
    }

    [Fact]
    public async Task ClosingDirtyDocumentSavesWithoutReloadingTheDocumentList()
    {
        var service = new SuccessfulKnowledgeDocumentService();
        var dialogs = new FakeDialogs { Choice = UnsavedChangesChoice.Save };
        var viewModel = new KnowledgeDocumentsViewModel(service, new FakeImageStorage(), dialogs);
        viewModel.RegisterEditorSnapshotProvider(() => "draft");
        viewModel.Title = "Tài liệu cần lưu";
        viewModel.MarkEditorChanged();

        var canClose = await viewModel.CanCloseAsync(refreshAfterSave: false);

        Assert.True(canClose);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(1, service.SaveCount);
        Assert.Equal(1, service.SearchCount);
    }

    private sealed class SuccessfulKnowledgeDocumentService : IKnowledgeDocumentService
    {
        public int SaveCount { get; private set; }
        public int SearchCount { get; private set; }

        public Task<IReadOnlyList<KnowledgeDocumentListItem>> SearchAsync(string? title, CancellationToken cancellationToken = default)
        {
            SearchCount++;
            return Task.FromResult<IReadOnlyList<KnowledgeDocumentListItem>>(Array.Empty<KnowledgeDocumentListItem>());
        }

        public Task<KnowledgeDocumentDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeDocumentDetails?>(null);

        public Task<KnowledgeDocumentSaveResult> SaveAsync(int? id, string title, string content, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(KnowledgeDocumentSaveResult.Success(id ?? 1));
        }
    }

    private sealed class FailingKnowledgeDocumentService : IKnowledgeDocumentService
    {
        public string? LastContent { get; private set; }
        public Task<IReadOnlyList<KnowledgeDocumentListItem>> SearchAsync(string? title, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeDocumentListItem>>(Array.Empty<KnowledgeDocumentListItem>());
        public Task<KnowledgeDocumentDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeDocumentDetails?>(null);
        public Task<KnowledgeDocumentSaveResult> SaveAsync(int? id, string title, string content, CancellationToken cancellationToken = default)
        {
            LastContent = content;
            return Task.FromResult(KnowledgeDocumentSaveResult.Failure("Database unavailable"));
        }
    }

    private sealed class FakeImageStorage : IKnowledgeImageStorage
    {
        public Task<StoredKnowledgeImage> StoreAsync(string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredKnowledgeImage(Path.GetFileName(sourcePath), sourcePath));
    }

    private sealed class FakeDialogs : IUserDialogService
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int MessageCount { get; private set; }
        public int ConfirmationCount { get; private set; }
        public bool Confirm(string title, string message) => true;
        public UnsavedChangesChoice AskToSaveChanges(string itemName)
        {
            ConfirmationCount++;
            return Choice;
        }
        public void ShowMessage(string title, string message, bool isError = false) => MessageCount++;
    }
}
