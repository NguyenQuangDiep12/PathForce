using PathForge.Domain.Entities;
using EvidenceEntity = PathForge.Domain.Entities.Evidence;

namespace PathForge.Application.Evidence;

public sealed class EvidenceService : IEvidenceService
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".webp" };
    private readonly IEvidenceRepository _repository;
    private readonly IFileStorage _fileStorage;

    public EvidenceService(IEvidenceRepository repository, IFileStorage fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<EvidenceDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByProblemAsync(problemId, cancellationToken);
        return items.Select(ToDetails).ToList();
    }

    public async Task<(bool Succeeded, int? Id, string? Error)> AddAsync(
        int problemId, int? workLogId, string sourcePath, string? description, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(sourcePath);
        if (!SupportedExtensions.Contains(extension))
            return (false, null, "Chỉ hỗ trợ ảnh PNG, JPG, JPEG hoặc WebP.");

        try
        {
            if (_fileStorage.GetLength(sourcePath) > MaxFileBytes)
                return (false, null, "Mỗi ảnh phải có dung lượng tối đa 5 MB.");
            if (!_fileStorage.HasSupportedImageSignature(sourcePath))
                return (false, null, "Tệp đã chọn không phải ảnh PNG, JPG, JPEG hoặc WebP hợp lệ.");
            if (workLogId.HasValue && !await _repository.WorkLogBelongsToProblemAsync(workLogId.Value, problemId, cancellationToken))
                return (false, null, "Phiên làm việc không thuộc bài toán đã chọn.");

            var relativePath = await _fileStorage.CopyIntoEvidenceFolderAsync(sourcePath, cancellationToken);
            try
            {
                var evidence = new EvidenceEntity
                {
                    ProblemId = problemId,
                    WorkLogId = workLogId,
                    DisplayName = Path.GetFileName(sourcePath),
                    RelativePath = relativePath,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                var id = await _repository.AddAsync(evidence, cancellationToken);
                return (true, id, null);
            }
            catch
            {
                await _fileStorage.DeleteAsync(relativePath, cancellationToken);
                throw;
            }
        }
        catch (FileNotFoundException)
        {
            return (false, null, "Không tìm thấy tệp ảnh đã chọn.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, null, "Không thể đọc hoặc lưu ảnh. Hãy kiểm tra quyền truy cập thư mục.");
        }
        catch (Exception)
        {
            return (false, null, "Không thể lưu ảnh minh chứng. Bản ghi chưa được thêm; hãy thử lại.");
        }
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var evidence = await _repository.GetByIdAsync(id, cancellationToken);
        if (evidence is null)
            return (false, "Không tìm thấy ảnh minh chứng.");
        try
        {
            await _fileStorage.DeleteAsync(evidence.RelativePath, cancellationToken);
        }
        catch (Exception)
        {
            return (false, "Không xóa được tệp ảnh. Bản ghi vẫn được giữ lại để bạn thử lại.");
        }
        await _repository.DeleteAsync(id, cancellationToken);
        return (true, null);
    }

    private EvidenceDetails ToDetails(EvidenceEntity evidence) => new(
        evidence.Id, evidence.ProblemId, evidence.WorkLogId, evidence.DisplayName,
        _fileStorage.GetAbsolutePath(evidence.RelativePath), evidence.Description, evidence.CreatedAt);
}
