namespace EKvarovi.Api.Services.Storage;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName,
        int faultReportId, CancellationToken ct);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct);

    Task DeleteAsync(string relativePath, CancellationToken ct);
}

public sealed record StoredFile(string StoredFileName, string RelativePath, long SizeBytes);
