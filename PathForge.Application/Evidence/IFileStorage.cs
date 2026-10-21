namespace PathForge.Application.Evidence;

public interface IFileStorage
{
    long GetLength(string path);
    bool HasSupportedImageSignature(string path);
    Task<string> CopyIntoEvidenceFolderAsync(string sourcePath, CancellationToken cancellationToken = default);
    string GetAbsolutePath(string relativePath);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
