using Microsoft.JSInterop;

namespace EKvarovi.App.Services;

public sealed class AttachmentMediaService(IHttpClientFactory httpClientFactory, IJSRuntime js)
    : IAttachmentMediaService
{
    public async Task<string?> GetBlobUrlAsync(string downloadUrl, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("Api");
        var path = downloadUrl.TrimStart('/');

        using var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return await js.InvokeAsync<string>("attachmentMedia.createObjectUrl", ct, bytes, contentType);
    }

    public async Task DownloadAsync(string downloadUrl, string fileName, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("Api");
        var path = downloadUrl.TrimStart('/');

        using var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            return;

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        await js.InvokeVoidAsync("attachmentMedia.downloadBlob", ct, bytes, contentType, fileName);
    }

    public ValueTask RevokeBlobUrlAsync(string blobUrl)
        => js.InvokeVoidAsync("attachmentMedia.revokeObjectUrl", blobUrl);
}
