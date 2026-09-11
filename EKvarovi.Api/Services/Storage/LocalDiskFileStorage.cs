using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EKvarovi.Api.Services.Storage;

public sealed class LocalDiskFileStorage(
    IOptions<FileStorageOptions> options,
    IHostEnvironment environment) : IFileStorage
{
    private readonly string _rootPath = ResolveRootPath(options.Value, environment);

    public async Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, int faultReportId, CancellationToken ct)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var year = DateTime.UtcNow.Year;
        var relativePath = $"{year}/{faultReportId}/{storedFileName}";

        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        await content.CopyToAsync(fileStream, ct);
        var sizeBytes = fileStream.Length;

        return new StoredFile(storedFileName, relativePath, sizeBytes);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        ValidateRelativePath(relativePath);

        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
            throw AppException.NotFound("Datoteka nije pronađena.");

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        ValidateRelativePath(relativePath);

        var fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private string GetFullPath(string relativePath)
    {
        ValidateRelativePath(relativePath);
        return Path.GetFullPath(Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || relativePath.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw AppException.Validation("Neispravna putanja datoteke.");
        }
    }

    private static string ResolveRootPath(FileStorageOptions options, IHostEnvironment environment)
    {
        var rootPath = options.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new InvalidOperationException("FileStorage:RootPath mora biti postavljen.");

        if (!Path.IsPathRooted(rootPath))
            rootPath = Path.Combine(environment.ContentRootPath, rootPath);

        return Path.GetFullPath(rootPath);
    }
}
