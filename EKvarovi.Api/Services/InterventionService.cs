using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Dtos.Interventions;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class InterventionService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHistoryService history) : IInterventionService
{
    private const string ActiveAssignmentNotFoundMessage = "Aktivna dodjela nije pronađena.";
    private const string InterventionNotFoundMessage = "Intervencija nije pronađena.";

    private static readonly Dictionary<string, Expression<Func<Intervention, object>>> SortMap = new()
    {
        ["startedAt"] = i => i.StartedAt!,
        ["finishedAt"] = i => i.FinishedAt!,
        ["status"] = i => i.InterventionStatusId,
        ["reportNumber"] = i => i.FaultReport.ReportNumber,
        ["technician"] = i => i.Technician.LastName,
        ["location"] = i => i.FaultReport.Location.Name,
        ["priority"] = i => i.FaultReport.FaultPriorityId!
    };

    public Task<PagedResult<InterventionListDto>> SearchAsync(
        InterventionFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: false, ct);

    public Task<PagedResult<InterventionListDto>> GetMineAsync(
        InterventionFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: true, ct);

    private async Task<PagedResult<InterventionListDto>> SearchAsync(
        InterventionFilterDto f, bool onlyMine, CancellationToken ct)
    {
        var startedFrom = DateTimeNormalizer.ToUtc(f.StartedFrom);
        var startedTo = DateTimeNormalizer.ToUtc(f.StartedTo);

        if (startedFrom > startedTo)
            throw AppException.Validation("Početni datum ne smije biti nakon završnog.");

        var now = DateTime.UtcNow;
        var query = db.Interventions.AsNoTracking();

        if (onlyMine)
            query = query.Where(i => i.TechnicianUserId == currentUser.UserId);
        else if (f.TechnicianUserId is int tech)
            query = query.Where(i => i.TechnicianUserId == tech);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = $"%{f.Search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.FaultReport.ReportNumber, term) ||
                (i.Note != null && EF.Functions.ILike(i.Note, term)));
        }

        if (f.InterventionStatusId is int status)
            query = query.Where(i => i.InterventionStatusId == status);

        if (startedFrom is DateTime from)
            query = query.Where(i => i.StartedAt >= from);

        if (startedTo is DateTime to)
            query = query.Where(i => i.StartedAt <= to);

        return await ProjectList(
                query.ApplySort(f.SortBy, f.SortDir, SortMap, defaultKey: "startedAt"),
                now)
            .ToPagedResultAsync(f, ct);
    }

    public async Task<IReadOnlyList<InterventionListDto>> GetByFaultReportIdAsync(
        int faultReportId, CancellationToken ct = default)
    {
        var canView = await db.FaultReports
            .AsNoTracking()
            .VisibleTo(currentUser)
            .AnyAsync(r => r.Id == faultReportId, ct);

        if (!canView)
            throw AppException.NotFound("Prijava nije pronađena.");

        var now = DateTime.UtcNow;

        return await ProjectList(
                db.Interventions.AsNoTracking()
                    .Where(i => i.FaultReportId == faultReportId)
                    .OrderByDescending(i => i.StartedAt),
                now)
            .ToListAsync(ct);
    }

    public async Task<InterventionDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var faultReportId = await db.Interventions
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => (int?)i.FaultReportId)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        var visible = await db.FaultReports
            .AsNoTracking()
            .VisibleTo(currentUser)
            .AnyAsync(r => r.Id == faultReportId, ct);

        if (!visible)
            throw AppException.Forbidden("Nemate ovlasti za pregled ove intervencije.");

        return await GetDetailAsync(id, ct);
    }

    public async Task<InterventionDetailDto> CreateAsync(InterventionCreateDto dto, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var assignment = await db.WorkAssignments
            .Include(a => a.FaultReport)
            .FirstOrDefaultAsync(a => a.Id == dto.WorkAssignmentId && a.IsActive, ct)
            ?? throw AppException.NotFound(ActiveAssignmentNotFoundMessage);

        if (!currentUser.IsInRole(Roles.Admin)
            && assignment.TechnicianUserId != currentUser.UserId)
            throw AppException.Forbidden("Intervenciju smije pokrenuti samo izvršitelj s aktivne dodjele.");

        if (assignment.FaultReport.FaultStatusId is FaultStatusIds.Rijeseno or FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Intervenciju nije moguće pokrenuti u trenutnom statusu prijave.");

        var hasRunning = await db.Interventions
            .AnyAsync(i => i.FaultReportId == assignment.FaultReportId
                           && i.InterventionStatusId == InterventionStatusIds.UTijeku, ct);

        if (hasRunning)
            throw AppException.Conflict("Prijava već ima intervenciju u tijeku.");

        var now = DateTime.UtcNow;

        var intervention = new Intervention
        {
            WorkAssignmentId = assignment.Id,
            FaultReportId = assignment.FaultReportId,
            TechnicianUserId = assignment.TechnicianUserId,
            InterventionStatusId = InterventionStatusIds.UTijeku,
            StartedAt = now,
            Note = dto.Note
        };
        db.Interventions.Add(intervention);

        ChangeStatus(assignment.FaultReport, FaultStatusIds.URadu);

        history.Add(assignment.FaultReportId, HistoryChangeType.InterventionStarted,
            oldValue: null,
            newValue: assignment.TechnicianUserId.ToString(),
            note: dto.Note);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetDetailAsync(intervention.Id, ct);
    }

    public async Task<InterventionDetailDto> UpdateAsync(
        int id, InterventionUpdateDto dto, CancellationToken ct = default)
    {
        var intervention = await db.Interventions
            .Include(i => i.WorkAssignment)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        EnsureCanModify(intervention);

        intervention.Note = dto.Note;

        await db.SaveChangesAsync(ct);

        return await GetDetailAsync(id, ct);
    }

    public async Task<InterventionDetailDto> FinishAsync(
        int id, InterventionFinishDto dto, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var intervention = await db.Interventions
            .Include(i => i.WorkAssignment)
            .Include(i => i.FaultReport)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        EnsureCanModify(intervention);

        var finishedAt = DateTimeNormalizer.ToUtc(dto.FinishedAt) ?? DateTime.UtcNow;

        if (intervention.StartedAt is null)
            throw AppException.Validation("Intervencija nema zabilježeno vrijeme početka.");
        if (finishedAt < intervention.StartedAt || finishedAt > DateTime.UtcNow)
            throw AppException.Validation("Vrijeme završetka mora biti nakon početka i ne u budućnosti.");
        if (string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Trim().Length < 10)
            throw AppException.Validation("Bilješka je obavezna i mora imati barem 10 znakova.");
        if (!dto.IsSuccessful && string.IsNullOrWhiteSpace(dto.FailureReason))
            throw AppException.Validation("Razlog neuspjeha je obavezan.");

        intervention.FinishedAt = finishedAt;
        intervention.Note = dto.Note.Trim();
        intervention.InterventionStatusId = dto.IsSuccessful
            ? InterventionStatusIds.Zavrsena
            : InterventionStatusIds.Neuspjesna;
        intervention.FailureReason = dto.IsSuccessful ? null : dto.FailureReason!.Trim();

        if (dto.IsSuccessful)
        {
            intervention.FaultReport.ResolvedAt = finishedAt;
            ChangeStatus(intervention.FaultReport, FaultStatusIds.Rijeseno);
        }

        history.Add(intervention.FaultReportId, HistoryChangeType.InterventionFinished,
            oldValue: null,
            newValue: dto.IsSuccessful ? "Uspješna" : "Neuspješna",
            note: dto.Note);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetDetailAsync(id, ct);
    }

    private void EnsureCanModify(Intervention intervention)
    {
        if (!currentUser.IsInRole(Roles.Admin)
            && (intervention.WorkAssignment.TechnicianUserId != currentUser.UserId
                || !intervention.WorkAssignment.IsActive))
            throw AppException.Forbidden("Možete mijenjati samo vlastiti aktivni nalog.");

        if (intervention.InterventionStatusId is InterventionStatusIds.Zavrsena
                                              or InterventionStatusIds.Neuspjesna)
            throw AppException.Conflict("Završena intervencija se više ne može mijenjati.");
    }

    private async Task<InterventionDetailDto> GetDetailAsync(int id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        return await db.Interventions
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InterventionDetailDto
            {
                Id = i.Id,
                WorkAssignmentId = i.WorkAssignmentId,
                FaultReportId = i.FaultReportId,
                TechnicianUserId = i.TechnicianUserId,
                InterventionStatusId = i.InterventionStatusId,
                StartedAt = i.StartedAt,
                FinishedAt = i.FinishedAt,
                Note = i.Note,
                FailureReason = i.FailureReason,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                StatusName = i.InterventionStatus.Name,
                TechnicianName = i.Technician.FirstName + " " + i.Technician.LastName,
                ReportNumber = i.FaultReport.ReportNumber,
                Title = i.FaultReport.Title,
                LocationName = i.FaultReport.Location.Name,
                FaultStatusId = i.FaultReport.FaultStatusId,
                FaultStatusName = i.FaultReport.FaultStatus.Name,
                PriorityName = i.FaultReport.FaultPriority != null ? i.FaultReport.FaultPriority.Name : null,
                PriorityColor = i.FaultReport.FaultPriority != null ? i.FaultReport.FaultPriority.ColorHex : null,
                DueDate = i.FaultReport.DueDate,
                IsOverdue = i.FaultReport.DueDate != null && i.FaultReport.DueDate < now
                            && i.FaultReport.FaultStatusId != FaultStatusIds.Rijeseno
                            && i.FaultReport.FaultStatusId != FaultStatusIds.Zatvoreno,
                TotalMaterialCost = db.InterventionMaterials
                    .Where(im => im.InterventionId == i.Id)
                    .Sum(im => im.Quantity * im.UnitPriceSnapshot),
                PhotosAfter = db.Attachments
                    .Where(a => a.InterventionId == i.Id
                                && a.Purpose == AttachmentPurpose.PhotoAfter)
                    .OrderBy(a => a.UploadedAt)
                    .Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        OriginalFileName = a.OriginalFileName,
                        Purpose = a.Purpose,
                        ContentType = a.ContentType,
                        SizeBytes = a.SizeBytes,
                        DownloadUrl = "/api/attachments/" + a.Id,
                        UploadedByUserId = a.UploadedByUserId,
                        UploadedByUserName = a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName,
                        UploadedAt = a.UploadedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);
    }

    private void ChangeStatus(FaultReport report, int newStatusId)
    {
        if (report.FaultStatusId == newStatusId)
            return;

        FaultReportStatusTransitions.EnsureAllowed(report.FaultStatusId, newStatusId);

        var oldStatusId = report.FaultStatusId;
        report.FaultStatusId = newStatusId;
        history.Add(report.Id, HistoryChangeType.StatusChanged,
            oldStatusId.ToString(), newStatusId.ToString(), null);
    }

    private static IQueryable<InterventionListDto> ProjectList(IQueryable<Intervention> query, DateTime now)
        => query.Select(i => new InterventionListDto
        {
            Id = i.Id,
            FaultReportId = i.FaultReportId,
            WorkAssignmentId = i.WorkAssignmentId,
            ReportNumber = i.FaultReport.ReportNumber,
            Title = i.FaultReport.Title,
            LocationName = i.FaultReport.Location.Name,
            PriorityName = i.FaultReport.FaultPriority != null ? i.FaultReport.FaultPriority.Name : null,
            PriorityColor = i.FaultReport.FaultPriority != null ? i.FaultReport.FaultPriority.ColorHex : null,
            TechnicianName = i.Technician.FirstName + " " + i.Technician.LastName,
            InterventionStatusId = i.InterventionStatusId,
            StatusName = i.InterventionStatus.Name,
            StartedAt = i.StartedAt,
            FinishedAt = i.FinishedAt,
            Note = i.Note
        });
}
