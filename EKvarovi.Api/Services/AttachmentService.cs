using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Infrastructure.Options;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Api.Services.Storage;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EKvarovi.Api.Services;

public sealed class AttachmentService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IFileStorage fileStorage,
    IOptions<FileStorageOptions> fileStorageOptions) : IAttachmentService
{
    private const int MaxAttachmentsPerPurpose = 5;
    private const string AttachmentNotFoundMessage = "Privitak nije pronađen.";
    private const string FaultReportNotFoundMessage = "Prijava nije pronađena.";
    private const string InterventionNotFoundMessage = "Intervencija nije pronađena.";
    private const string LimitReachedMessage =
        "Dosegnut je maksimalan broj privitaka ove namjene (5).";
    private const string PurposeMismatchMessage =
        "Namjena privitka ne odgovara odabranom zapisu.";
    private const string UploadForbiddenMessage =
        "Nemate ovlasti za upload privitka na ovu prijavu.";
    private const string InterventionUploadForbiddenMessage =
        "Nemate ovlasti za upload privitka na ovu intervenciju.";
    private const string DeleteForbiddenMessage =
        "Nemate ovlasti za brisanje ovog privitka.";
    private const string PhotoMustBeImageMessage =
        "Fotografija mora biti slika (jpg, jpeg, png ili webp).";
    private const string DocumentMustBePdfMessage =
        "Dokument mora biti u PDF formatu.";

    public async Task<AttachmentDto> UploadToFaultReportAsync(
        int faultReportId, AttachmentPurpose purpose, IFormFile file, CancellationToken ct = default)
    {
        EnsureFaultReportPurpose(purpose);
        EnsureFileProvided(file);
        await FileUploadValidator.ValidateAsync(file, fileStorageOptions.Value, ct);
        EnsureFileTypeMatchesPurpose(purpose, file);

        var report = await db.FaultReports
            .FirstOrDefaultAsync(r => r.Id == faultReportId, ct)
            ?? throw AppException.NotFound(FaultReportNotFoundMessage);

        EnsureCanUploadToFaultReport(report);
        await EnsureLimitNotReachedAsync(faultReportId, purpose, interventionId: null, ct);

        return await SaveAttachmentAsync(
            file,
            purpose,
            faultReportId,
            interventionId: null,
            ct);
    }

    public async Task<AttachmentDto> UploadToInterventionAsync(
        int interventionId, IFormFile file, CancellationToken ct = default)
    {
        EnsureFileProvided(file);
        await FileUploadValidator.ValidateAsync(file, fileStorageOptions.Value, ct);
        EnsureFileTypeMatchesPurpose(AttachmentPurpose.PhotoAfter, file);

        var intervention = await db.Interventions
            .FirstOrDefaultAsync(i => i.Id == interventionId, ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        EnsureCanUploadToIntervention(intervention);
        await EnsureLimitNotReachedAsync(
            faultReportId: null,
            AttachmentPurpose.PhotoAfter,
            interventionId,
            ct);

        return await SaveAttachmentAsync(
            file,
            AttachmentPurpose.PhotoAfter,
            intervention.FaultReportId,
            interventionId,
            ct);
    }

    public async Task<AttachmentDownloadResult> OpenForDownloadAsync(
        int attachmentId, CancellationToken ct = default)
    {
        var attachment = await db.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
            ?? throw AppException.NotFound(AttachmentNotFoundMessage);

        var faultReportId = attachment.FaultReportId
            ?? await db.Interventions
                .AsNoTracking()
                .Where(i => i.Id == attachment.InterventionId)
                .Select(i => (int?)i.FaultReportId)
                .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(AttachmentNotFoundMessage);

        var visible = await db.FaultReports
            .AsNoTracking()
            .VisibleTo(currentUser)
            .AnyAsync(r => r.Id == faultReportId, ct);

        if (!visible)
            throw AppException.Forbidden("Nemate ovlasti za preuzimanje ovog privitka.");

        var stream = await fileStorage.OpenReadAsync(attachment.RelativePath, ct);
        return new AttachmentDownloadResult(
            attachment.ContentType,
            attachment.OriginalFileName,
            stream);
    }

    public async Task DeleteAsync(int attachmentId, CancellationToken ct = default)
    {
        var attachment = await db.Attachments
            .Include(a => a.UploadedByUser)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
            ?? throw AppException.NotFound(AttachmentNotFoundMessage);

        EnsureCanDelete(attachment);

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<AttachmentDto> SaveAttachmentAsync(
        IFormFile file,
        AttachmentPurpose purpose,
        int faultReportId,
        int? interventionId,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var stored = await fileStorage.SaveAsync(stream, file.FileName, faultReportId, ct);

        try
        {
            var uploaderName = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUser.UserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstAsync(ct);

            var attachment = new Attachment
            {
                Purpose = purpose,
                FaultReportId = interventionId.HasValue ? null : faultReportId,
                InterventionId = interventionId,
                OriginalFileName = file.FileName,
                StoredFileName = stored.StoredFileName,
                RelativePath = stored.RelativePath,
                ContentType = file.ContentType,
                SizeBytes = stored.SizeBytes,
                UploadedByUserId = currentUser.UserId,
                UploadedAt = DateTime.UtcNow
            };

            db.Attachments.Add(attachment);
            await db.SaveChangesAsync(ct);

            return MapToDto(attachment, uploaderName);
        }
        catch
        {
            await fileStorage.DeleteAsync(stored.RelativePath, ct);
            throw;
        }
    }

    private async Task EnsureLimitNotReachedAsync(
        int? faultReportId,
        AttachmentPurpose purpose,
        int? interventionId,
        CancellationToken ct)
    {
        var count = interventionId.HasValue
            ? await db.Attachments.CountAsync(
                a => a.InterventionId == interventionId && a.Purpose == purpose, ct)
            : await db.Attachments.CountAsync(
                a => a.FaultReportId == faultReportId && a.Purpose == purpose, ct);

        if (count >= MaxAttachmentsPerPurpose)
            throw AppException.Validation(LimitReachedMessage);
    }

    private static void EnsureFaultReportPurpose(AttachmentPurpose purpose)
    {
        if (purpose is not (AttachmentPurpose.PhotoBefore or AttachmentPurpose.Document))
            throw AppException.Validation(PurposeMismatchMessage);
    }

    private static void EnsureFileProvided(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw AppException.Validation("Datoteka nije priložena.");
    }

    private static void EnsureFileTypeMatchesPurpose(AttachmentPurpose purpose, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (purpose is AttachmentPurpose.PhotoBefore or AttachmentPurpose.PhotoAfter
            && extension == ".pdf")
        {
            throw AppException.Validation(PhotoMustBeImageMessage);
        }

        if (purpose == AttachmentPurpose.Document && extension != ".pdf")
            throw AppException.Validation(DocumentMustBePdfMessage);
    }

    private void EnsureCanUploadToFaultReport(FaultReport report)
    {
        if (currentUser.IsAdminOrManager)
            return;

        if (currentUser.IsInRole(Roles.Reporter) && report.ReportedByUserId == currentUser.UserId)
            return;

        throw AppException.Forbidden(UploadForbiddenMessage);
    }

    private void EnsureCanUploadToIntervention(Intervention intervention)
    {
        if (currentUser.IsInRole(Roles.Admin))
            return;

        if (intervention.TechnicianUserId == currentUser.UserId)
            return;

        throw AppException.Forbidden(InterventionUploadForbiddenMessage);
    }

    private void EnsureCanDelete(Attachment attachment)
    {
        if (currentUser.IsAdminOrManager)
            return;

        if (attachment.UploadedByUserId == currentUser.UserId)
            return;

        throw AppException.Forbidden(DeleteForbiddenMessage);
    }

    private static AttachmentDto MapToDto(Attachment attachment, string uploadedByUserName)
        => new()
        {
            Id = attachment.Id,
            OriginalFileName = attachment.OriginalFileName,
            Purpose = attachment.Purpose,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            DownloadUrl = $"/api/attachments/{attachment.Id}",
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedByUserName = uploadedByUserName,
            UploadedAt = attachment.UploadedAt
        };
}
