using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Dtos.FaultReports;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class FaultReportService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHistoryService history) : IFaultReportService
{
    private const string NotFoundMessage = "Prijava nije pronađena.";

    private static readonly Dictionary<string, Expression<Func<FaultReport, object>>> SortMap = new()
    {
        ["reportedAt"] = r => r.ReportedAt,
        ["dueDate"] = r => r.DueDate!,
        ["priority"] = r => r.FaultPriorityId!,
        ["status"] = r => r.FaultStatusId,
        ["location"] = r => r.Location.Name,
        ["reportNumber"] = r => r.ReportNumber
    };

    public Task<PagedResult<FaultReportListDto>> SearchAsync(
        FaultReportFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: false, ct);

    public Task<PagedResult<FaultReportListDto>> GetMineAsync(
        FaultReportFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: true, ct);

    private async Task<PagedResult<FaultReportListDto>> SearchAsync(
        FaultReportFilterDto f, bool onlyMine, CancellationToken ct)
    {
        var reportedFrom = DateTimeNormalizer.ToUtc(f.ReportedFrom);
        var reportedTo = DateTimeNormalizer.ToUtc(f.ReportedTo);

        if (reportedFrom > reportedTo)
            throw AppException.Validation("Početni datum ne smije biti nakon završnog.");

        var now = DateTime.UtcNow;

        var query = db.FaultReports.AsNoTracking().VisibleTo(currentUser);

        if (onlyMine)
            query = query.Where(r => r.ReportedByUserId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = $"%{f.Search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.ReportNumber, term) ||
                EF.Functions.ILike(r.Title, term) ||
                EF.Functions.ILike(r.Description, term));
        }

        if (f.LocationId is int loc)
            query = query.Where(r => r.LocationId == loc);

        if (f.FaultTypeId is int type)
            query = query.Where(r => r.FaultTypeId == type);

        if (f.FaultPriorityId is int prio)
            query = query.Where(r => r.FaultPriorityId == prio);

        if (f.FaultStatusId is int status)
            query = query.Where(r => r.FaultStatusId == status);

        if (reportedFrom is DateTime from)
            query = query.Where(r => r.ReportedAt >= from);

        if (reportedTo is DateTime to)
            query = query.Where(r => r.ReportedAt <= to);

        if (f.TechnicianUserId is int tech)
            query = query.Where(r => r.WorkAssignments
                .Any(a => a.IsActive && a.TechnicianUserId == tech));

        if (f.OnlyOverdue == true)
            query = query.Where(r => r.DueDate != null && r.DueDate < now
                                     && r.FaultStatusId != FaultStatusIds.Rijeseno
                                     && r.FaultStatusId != FaultStatusIds.Zatvoreno);

        return await query
            .ApplySort(f.SortBy, f.SortDir, SortMap, defaultKey: "reportedAt")
            .Select(r => new FaultReportListDto
            {
                Id = r.Id,
                ReportNumber = r.ReportNumber,
                Title = r.Title,
                LocationName = r.Location.Name,
                FaultTypeName = r.FaultType != null ? r.FaultType.Name : null,
                PriorityName = r.FaultPriority != null ? r.FaultPriority.Name : null,
                PriorityColor = r.FaultPriority != null ? r.FaultPriority.ColorHex : null,
                StatusName = r.FaultStatus.Name,
                DueDate = r.DueDate,
                IsOverdue = r.DueDate != null && r.DueDate < now
                            && r.FaultStatusId != FaultStatusIds.Rijeseno
                            && r.FaultStatusId != FaultStatusIds.Zatvoreno,
                TechnicianName = r.WorkAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.Technician.FirstName + " " + a.Technician.LastName)
                    .FirstOrDefault(),
                ReportedAt = r.ReportedAt
            })
            .ToPagedResultAsync(f, ct);
    }

    public async Task<FaultReportDetailDto> CreateAsync(FaultReportCreateDto dto, CancellationToken ct = default)
    {
        await ValidateCreateDtoAsync(dto, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var now = DateTime.UtcNow;
        var report = new FaultReport
        {
            ReportNumber = await GenerateReportNumberAsync(ct),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            LocationId = dto.LocationId,
            FaultStatusId = FaultStatusIds.Zaprimljeno,
            ReportedByUserId = currentUser.UserId,
            ReportedAt = now
        };

        db.FaultReports.Add(report);
        await db.SaveChangesAsync(ct);

        history.Add(report.Id, HistoryChangeType.Created, null, report.ReportNumber, null);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return await GetByIdAsync(report.Id, ct);
    }

    public async Task<FaultReportDetailDto> UpdateAsync(
        int id, FaultReportUpdateDto dto, CancellationToken ct = default)
    {
        var report = await db.FaultReports
            .VisibleTo(currentUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        EnsureCanUpdate(report);
        await ValidateBasicFieldsAsync(dto.Title, dto.Description, dto.LocationId, ct);

        report.Title = dto.Title.Trim();
        report.Description = dto.Description.Trim();
        report.LocationId = dto.LocationId;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var report = await db.FaultReports
            .VisibleTo(currentUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        EnsureCanDelete(report);

        if (await db.Interventions.AnyAsync(i => i.FaultReportId == id, ct))
            throw AppException.Conflict("Prijava s intervencijama ne može se obrisati.");

        db.FaultReports.Remove(report);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FaultReportDetailDto> TriageAsync(
        int id, FaultReportTriageDto dto, CancellationToken ct = default)
    {
        EnsureManagerOrAdmin();

        var report = await db.FaultReports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        if (report.FaultStatusId == FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Zatvorena prijava se ne može kategorizirati.");

        await ValidateTriageDtoAsync(dto, ct);

        var oldTypeId = report.FaultTypeId;
        var oldPriorityId = report.FaultPriorityId;
        var oldDueDate = report.DueDate;

        var dueDate = DateTimeNormalizer.ToUtc(dto.DueDate);

        report.FaultTypeId = dto.FaultTypeId;
        report.FaultPriorityId = dto.FaultPriorityId;
        report.DueDate = dueDate;

        if (report.FaultStatusId == FaultStatusIds.Zaprimljeno)
        {
            ChangeStatus(report, FaultStatusIds.Pregledano);
            if (report.ReviewedAt is null)
            {
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewedByUserId = currentUser.UserId;
            }
        }

        if (oldTypeId != dto.FaultTypeId)
        {
            history.Add(report.Id, HistoryChangeType.TypeChanged,
                oldTypeId?.ToString(), dto.FaultTypeId.ToString(), null);
        }

        if (oldPriorityId != dto.FaultPriorityId)
        {
            history.Add(report.Id, HistoryChangeType.PriorityChanged,
                oldPriorityId?.ToString(), dto.FaultPriorityId.ToString(), null);
        }

        if (oldDueDate != dueDate)
        {
            history.Add(report.Id, HistoryChangeType.DueDateChanged,
                oldDueDate?.ToString("O"), dueDate?.ToString("O"), null);
        }

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<FaultReportDetailDto> CloseAsync(
        int id, CloseFaultReportDto dto, CancellationToken ct = default)
    {
        EnsureManagerOrAdmin();

        if (string.IsNullOrWhiteSpace(dto.ClosingNote) || dto.ClosingNote.Trim().Length < 5)
            throw AppException.Validation("Napomena provjere je obavezna i mora imati barem 5 znakova.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var report = await db.FaultReports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        if (report.FaultStatusId == FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Zatvorena prijava se ne može mijenjati.");

        if (report.FaultStatusId != FaultStatusIds.Rijeseno)
            throw AppException.Validation("Zatvoriti se smije samo riješena prijava.");

        var hasSuccessfulIntervention = await db.Interventions
            .AnyAsync(i => i.FaultReportId == id
                           && i.InterventionStatusId == InterventionStatusIds.Zavrsena, ct);

        if (!hasSuccessfulIntervention)
            throw AppException.Conflict("Prijava se ne može zatvoriti bez barem jedne uspješne intervencije.");

        var now = DateTime.UtcNow;
        report.ClosedByUserId = currentUser.UserId;
        report.ClosedAt = now;
        report.ClosingNote = dto.ClosingNote.Trim();

        var activeAssignments = await db.WorkAssignments
            .Where(a => a.FaultReportId == id && a.IsActive)
            .ToListAsync(ct);

        foreach (var assignment in activeAssignments)
        {
            assignment.IsActive = false;
            assignment.UnassignedAt = now;
        }

        ChangeStatus(report, FaultStatusIds.Zatvoreno);

        history.Add(report.Id, HistoryChangeType.Closed,
            oldValue: FaultStatusIds.Rijeseno.ToString(),
            newValue: FaultStatusIds.Zatvoreno.ToString(),
            note: dto.ClosingNote.Trim());

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<FaultReportDetailDto> ReopenAsync(
        int id, ReopenFaultReportDto dto, CancellationToken ct = default)
    {
        EnsureManagerOrAdmin();

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 5)
            throw AppException.Validation("Obrazloženje je obavezno i mora imati barem 5 znakova.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var report = await db.FaultReports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        if (report.FaultStatusId != FaultStatusIds.Rijeseno)
            throw AppException.Validation("Vratiti u rad može se samo riješena prijava.");

        report.ResolvedAt = null;
        ChangeStatus(report, FaultStatusIds.URadu);

        var latestAssignment = await db.WorkAssignments
            .Where(a => a.FaultReportId == id)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync(ct);

        if (latestAssignment is { IsActive: false })
        {
            latestAssignment.IsActive = true;
            latestAssignment.UnassignedAt = null;
        }

        history.Add(report.Id, HistoryChangeType.Reopened,
            oldValue: FaultStatusIds.Rijeseno.ToString(),
            newValue: FaultStatusIds.URadu.ToString(),
            note: dto.Reason.Trim());

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<FaultReportDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await ProjectDetail(
                db.FaultReports.AsNoTracking()
                    .VisibleTo(currentUser)
                    .Where(r => r.Id == id))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(NotFoundMessage);

    private async Task<string> GenerateReportNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"KV-{year}-";

        var lastNumber = await db.FaultReports
            .IgnoreQueryFilters()
            .Where(r => r.ReportNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReportNumber)
            .Select(r => r.ReportNumber)
            .FirstOrDefaultAsync(ct);

        var next = lastNumber is null ? 1 : int.Parse(lastNumber[^6..]) + 1;
        return prefix + next.ToString("D6");
    }

    private IQueryable<FaultReportDetailDto> ProjectDetail(IQueryable<FaultReport> query)
    {
        var now = DateTime.UtcNow;

        return query.Select(r => new FaultReportDetailDto
        {
            Id = r.Id,
            ReportNumber = r.ReportNumber,
            Title = r.Title,
            Description = r.Description,
            LocationId = r.LocationId,
            LocationName = r.Location.Name,
            FaultTypeId = r.FaultTypeId,
            FaultTypeName = r.FaultType != null ? r.FaultType.Name : null,
            FaultPriorityId = r.FaultPriorityId,
            PriorityName = r.FaultPriority != null ? r.FaultPriority.Name : null,
            PriorityColor = r.FaultPriority != null ? r.FaultPriority.ColorHex : null,
            FaultStatusId = r.FaultStatusId,
            StatusName = r.FaultStatus.Name,
            DueDate = r.DueDate,
            IsOverdue = r.DueDate != null && r.DueDate < now
                        && r.FaultStatusId != FaultStatusIds.Rijeseno
                        && r.FaultStatusId != FaultStatusIds.Zatvoreno,
            ReportedByUserId = r.ReportedByUserId,
            ReportedByName = r.ReportedByUser.FirstName + " " + r.ReportedByUser.LastName,
            ReportedAt = r.ReportedAt,
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedByName = r.ReviewedByUser != null
                ? r.ReviewedByUser.FirstName + " " + r.ReviewedByUser.LastName
                : null,
            ReviewedAt = r.ReviewedAt,
            ResolvedAt = r.ResolvedAt,
            ClosedByUserId = r.ClosedByUserId,
            ClosedByName = r.ClosedByUser != null
                ? r.ClosedByUser.FirstName + " " + r.ClosedByUser.LastName
                : null,
            ClosedAt = r.ClosedAt,
            ClosingNote = r.ClosingNote,
            TotalMaterialCost = db.InterventionMaterials
                .Where(im => im.Intervention.FaultReportId == r.Id)
                .Sum(im => im.Quantity * im.UnitPriceSnapshot),
            PhotosBefore = db.Attachments
                .Where(a => a.FaultReportId == r.Id
                            && a.Purpose == AttachmentPurpose.PhotoBefore)
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
                .ToList(),
            Documents = db.Attachments
                .Where(a => a.FaultReportId == r.Id
                            && a.Purpose == AttachmentPurpose.Document)
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
                .ToList(),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });
    }

    private async Task ValidateCreateDtoAsync(FaultReportCreateDto dto, CancellationToken ct)
        => await ValidateBasicFieldsAsync(dto.Title, dto.Description, dto.LocationId, ct);

    private async Task ValidateBasicFieldsAsync(
        string? title, string? description, int locationId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmedTitle = title?.Trim() ?? string.Empty;
        var trimmedDescription = description?.Trim() ?? string.Empty;

        if (trimmedTitle.Length < 5 || trimmedTitle.Length > 200)
            errors["title"] = ["Naslov mora imati između 5 i 200 znakova."];

        if (trimmedDescription.Length < 10 || trimmedDescription.Length > 2000)
            errors["description"] = ["Opis mora imati između 10 i 2000 znakova."];

        if (locationId <= 0)
            errors["locationId"] = ["Lokacija je obavezna."];

        if (errors.Count > 0)
            throw AppException.Validation("Neispravni podaci prijave.", errors);

        var locationExists = await db.Locations
            .AnyAsync(l => l.Id == locationId && l.IsActive, ct);

        if (!locationExists)
            throw AppException.Validation("Odabrana lokacija ne postoji, nije aktivna ili je obrisana.");
    }

    private void EnsureCanUpdate(FaultReport report)
    {
        if (report.FaultStatusId == FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Zatvorena prijava se ne može mijenjati.");

        if (currentUser.IsAdminOrManager)
            return;

        if (report.ReportedByUserId == currentUser.UserId
            && report.FaultStatusId == FaultStatusIds.Zaprimljeno)
            return;

        throw AppException.Forbidden("Nemate ovlasti za izmjenu ove prijave.");
    }

    private void EnsureCanDelete(FaultReport report)
    {
        var isAdmin = currentUser.IsInRole(Roles.Admin);
        var isOwner = report.ReportedByUserId == currentUser.UserId;

        if (!isAdmin && !isOwner)
            throw AppException.Forbidden("Nemate ovlasti za brisanje ove prijave.");

        if (!isAdmin && report.FaultStatusId != FaultStatusIds.Zaprimljeno)
            throw AppException.Forbidden("Prijava se može brisati samo u statusu Zaprimljeno.");
    }

    private void EnsureManagerOrAdmin()
    {
        if (!currentUser.IsAdminOrManager)
            throw AppException.Forbidden("Kategorizaciju smiju izvoditi samo upravitelj i administrator.");
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

    private async Task ValidateTriageDtoAsync(FaultReportTriageDto dto, CancellationToken ct)
    {
        if (dto.FaultTypeId <= 0)
            throw AppException.Validation("Vrsta kvara je obavezna.");

        if (dto.FaultPriorityId <= 0)
            throw AppException.Validation("Prioritet je obavezan.");

        var typeExists = await db.FaultTypes
            .AnyAsync(t => t.Id == dto.FaultTypeId && t.IsActive, ct);

        if (!typeExists)
            throw AppException.Validation("Odabrana vrsta kvara ne postoji ili nije aktivna.");

        var priorityExists = await db.FaultPriorities
            .AnyAsync(p => p.Id == dto.FaultPriorityId && p.IsActive, ct);

        if (!priorityExists)
            throw AppException.Validation("Odabrani prioritet ne postoji ili nije aktivan.");

        var dueDate = DateTimeNormalizer.ToUtc(dto.DueDate);

        if (dto.FaultPriorityId == FaultPriorityIds.Kritican && dueDate is null)
            throw AppException.Validation("Kritičan prioritet zahtijeva rok.");

        if (dueDate is DateTime due && due < DateTime.UtcNow)
            throw AppException.Validation("Rok ne smije biti u prošlosti.");
    }
}
