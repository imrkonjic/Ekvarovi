namespace EKvarovi.App.Services;

public interface IAttachmentMediaService
{
    Task<string?> GetBlobUrlAsync(string downloadUrl, CancellationToken ct = default);

    Task DownloadAsync(string downloadUrl, string fileName, CancellationToken ct = default);

    ValueTask RevokeBlobUrlAsync(string blobUrl);
}
