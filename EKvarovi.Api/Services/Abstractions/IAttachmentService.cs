using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Services.Abstractions;

public interface IAttachmentService
{
    Task<AttachmentDto> UploadToFaultReportAsync(
        int faultReportId, AttachmentPurpose purpose, IFormFile file, CancellationToken ct = default);

    Task<AttachmentDto> UploadToInterventionAsync(
        int interventionId, IFormFile file, CancellationToken ct = default);

    Task<AttachmentDownloadResult> OpenForDownloadAsync(
        int attachmentId, CancellationToken ct = default);

    Task DeleteAsync(int attachmentId, CancellationToken ct = default);
}

public sealed record AttachmentDownloadResult(
    string ContentType,
    string OriginalFileName,
    Stream Stream);
