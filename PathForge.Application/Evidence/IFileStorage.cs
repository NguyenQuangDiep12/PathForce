namespace PathForge.Application.Evidence;

public interface IFileStorage
{
    long GetLength(string path);
    bool HasSupportedImageSignature(string path);
    Task<string> CopyIntoEvidenceFolderAsync(string sourcePath, CancellationToken cancellationToken = default);
    string GetAbsolutePath(string relativePath);
    Task<string?> StageDeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    Task RestoreStagedAsync(string stagedRelativePath, string originalRelativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
