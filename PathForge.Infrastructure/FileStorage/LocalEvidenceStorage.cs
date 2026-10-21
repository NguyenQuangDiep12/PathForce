using PathForge.Application.Evidence;

namespace PathForge.Infrastructure.FileStorage;

public sealed class LocalEvidenceStorage : IFileStorage
{
    private readonly string _evidenceDirectory;

    public LocalEvidenceStorage(string evidenceDirectory)
    {
        _evidenceDirectory = Path.GetFullPath(evidenceDirectory);
        Directory.CreateDirectory(_evidenceDirectory);
    }

    public long GetLength(string path) => new FileInfo(path).Length;

    public bool HasSupportedImageSignature(string path)
    {
        using var stream = File.OpenRead(path);
        var header = new byte[12];
        var count = 0;
        while (count < header.Length)
        {
            var read = stream.Read(header, count, header.Length - count);
            if (read == 0)
                break;
            count += read;
        }
        var isPng = count >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E &&
                    header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
        var isJpeg = count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isWebP = count >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
                     header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
        return isPng || isJpeg || isWebP;
    }

    public Task<string> CopyIntoEvidenceFolderAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source image was not found.", sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var name = Guid.NewGuid().ToString("N") + extension;
        var targetPath = Path.Combine(_evidenceDirectory, name);
        File.Copy(sourcePath, targetPath, overwrite: false);
        return Task.FromResult(name);
    }

    public string GetAbsolutePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_evidenceDirectory, relativePath));
        var prefix = _evidenceDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Evidence path is outside the PathForge data folder.");
        return fullPath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetAbsolutePath(relativePath);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}
