using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Infrastructure.Options;

namespace EKvarovi.Api.Services.Storage;

public static class FileUploadValidator
{
    private static readonly Dictionary<string, (string Mime, byte[][] Signatures)> Allowed = new()
    {
        [".jpg"] = ("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
        [".jpeg"] = ("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
        [".png"] = ("image/png", [[0x89, 0x50, 0x4E, 0x47]]),
        [".webp"] = ("image/webp", [[0x52, 0x49, 0x46, 0x46]]),
        [".pdf"] = ("application/pdf", [[0x25, 0x50, 0x44, 0x46]])
    };

    public static async Task ValidateAsync(IFormFile file, FileStorageOptions options, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!Allowed.TryGetValue(extension, out var spec))
            throw AppException.UnsupportedMedia("Dopušteni su samo jpg, jpeg, png, webp i pdf.");

        if (!string.Equals(file.ContentType, spec.Mime, StringComparison.OrdinalIgnoreCase))
            throw AppException.UnsupportedMedia("Tip datoteke ne odgovara ekstenziji.");

        var maxSize = extension == ".pdf"
            ? options.MaxDocumentSizeBytes
            : options.MaxImageSizeBytes;
        if (file.Length > maxSize)
            throw AppException.TooLarge(
                $"Datoteka premašuje dopuštenih {maxSize / 1024 / 1024} MB.");

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

        if (extension == ".webp")
        {
            if (read < 12
                || header[0] != 0x52 || header[1] != 0x49 || header[2] != 0x46 || header[3] != 0x46
                || header[8] != (byte)'W' || header[9] != (byte)'E' || header[10] != (byte)'B'
                || header[11] != (byte)'P')
            {
                throw AppException.UnsupportedMedia("Sadržaj datoteke ne odgovara navedenom tipu.");
            }
        }
        else if (!spec.Signatures.Any(sig =>
                     read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig)))
        {
            throw AppException.UnsupportedMedia("Sadržaj datoteke ne odgovara navedenom tipu.");
        }
    }
}
