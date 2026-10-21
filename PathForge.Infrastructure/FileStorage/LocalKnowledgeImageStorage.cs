using System.Security.Cryptography;
using PathForge.Application.KnowledgeDocuments;

namespace PathForge.Infrastructure.FileStorage;

public sealed class LocalKnowledgeImageStorage : IKnowledgeImageStorage
{
    public const long MaximumImageBytes = 10 * 1024 * 1024;
    private readonly string _directory;

    public LocalKnowledgeImageStorage(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public async Task<StoredKnowledgeImage> StoreAsync(
        string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("Không tìm thấy ảnh cần chèn.", sourcePath);

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg")
            throw new InvalidDataException("Chỉ hỗ trợ ảnh PNG, JPG và JPEG.");
        if (extension == ".jpeg")
            extension = ".jpg";

        var info = new FileInfo(sourcePath);
        if (info.Length <= 0 || info.Length > MaximumImageBytes)
            throw new InvalidDataException("Ảnh phải có dung lượng từ 1 byte đến 10 MB.");

        var bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        if (bytes.Length <= 0 || bytes.Length > MaximumImageBytes || !HasMatchingSignature(extension, bytes))
            throw new InvalidDataException("Tệp không phải ảnh PNG/JPG hợp lệ hoặc đã vượt quá 10 MB.");

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var fileName = hash + extension;
        var destination = Path.Combine(_directory, fileName);
        if (!File.Exists(destination))
        {
            var temporaryPath = Path.Combine(_directory, $".{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                try
                {
                    File.Move(temporaryPath, destination);
                }
                catch (IOException) when (File.Exists(destination))
                {
                    // An identical image was copied by another PathForge instance.
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        return new StoredKnowledgeImage(fileName, destination);
    }

    private static bool HasMatchingSignature(string extension, byte[] bytes)
    {
        if (extension == ".png")
        {
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            return bytes.Length >= signature.Length && signature.SequenceEqual(bytes.Take(signature.Length));
        }

        return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
    }
}
