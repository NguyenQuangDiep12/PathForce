namespace PathForge.Application.KnowledgeDocuments;

public sealed record StoredKnowledgeImage(string RelativePath, string FullPath);

public interface IKnowledgeImageStorage
{
    Task<StoredKnowledgeImage> StoreAsync(string sourcePath, CancellationToken cancellationToken = default);
}
