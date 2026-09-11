using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Entities;

public class Attachment
{
    public int Id { get; set; }
    public AttachmentPurpose Purpose { get; set; }
    public int? FaultReportId { get; set; }
    public int? InterventionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public FaultReport? FaultReport { get; set; }
    public Intervention? Intervention { get; set; }
    public User UploadedByUser { get; set; } = null!;
}
