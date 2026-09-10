namespace EKvarovi.Api.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "";
    public long MaxImageSizeBytes { get; set; }
    public long MaxDocumentSizeBytes { get; set; }
}
