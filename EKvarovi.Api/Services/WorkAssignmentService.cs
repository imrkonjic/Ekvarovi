using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Assignments;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class WorkAssignmentService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHistoryService history) : IWorkAssignmentService
{
    private const string AssignmentNotFoundMessage = "Radni nalog nije pronađen.";

    private static readonly Dictionary<string, Expression<Func<WorkAssignment, object>>> SortMap = new()
    {
        ["assignedAt"] = a => a.AssignedAt,
        ["dueDate"] = a => a.FaultReport.DueDate!,
        ["priority"] = a => a.FaultReport.FaultPriorityId!,
        ["location"] = a => a.FaultReport.Location.Name,
        ["reportNumber"] = a => a.FaultReport.ReportNumber,
        ["status"] = a => a.FaultReport.FaultStatusId,
        ["isActive"] = a => a.IsActive
    };

    public Task<PagedResult<AssignmentListDto>> SearchAsync(
        AssignmentFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: false, ct);

    public Task<PagedResult<AssignmentListDto>> GetMineAsync(
        AssignmentFilterDto filter, CancellationToken ct = default)
        => SearchAsync(filter, onlyMine: true, ct);

    private async Task<PagedResult<AssignmentListDto>> SearchAsync(
        AssignmentFilterDto f, bool onlyMine, CancellationToken ct)
    {
        var assignedFrom = DateTimeNormalizer.ToUtc(f.AssignedFrom);
        var assignedTo = DateTimeNormalizer.ToUtc(f.AssignedTo);

        if (assignedFrom > assignedTo)
            throw AppException.Validation("Početni datum ne smije biti nakon završnog.");

        var now = DateTime.UtcNow;
        var query = db.WorkAssignments.AsNoTracking();

        if (onlyMine)
        {
            query = query.Where(a => a.TechnicianUserId == currentUser.UserId);

            if (f.OnlyActive ?? true)
                query = query.Where(a => a.IsActive);
        }
        else
        {
            if (f.TechnicianUserId is int tech)
                query = query.Where(a => a.TechnicianUserId == tech);

            if (f.IsActive is bool isActive)
                query = query.Where(a => a.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = $"%{f.Search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.FaultReport.ReportNumber, term) ||
                EF.Functions.ILike(a.FaultReport.Title, term));
        }

        if (f.LocationId is int loc)
            query = query.Where(a => a.FaultReport.LocationId == loc);

        if (assignedFrom is DateTime from)
            query = query.Where(a => a.AssignedAt >= from);

        if (assignedTo is DateTime to)
            query = query.Where(a => a.AssignedAt <= to);

        var defaultSort = onlyMine ? "priority" : "assignedAt";

        return await ProjectList(
                query.ApplySort(f.SortBy, f.SortDir, SortMap, defaultSort),
                now)
            .ToPagedResultAsync(f, ct);
    }

    public async Task<AssignmentDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var technicianUserId = await db.WorkAssignments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => (int?)a.TechnicianUserId)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(AssignmentNotFoundMessage);

        EnsureCanView(technicianUserId);

        return await GetDetailAsync(id, ct);
    }

    public async Task<IReadOnlyList<AssignmentListDto>> GetByFaultReportIdAsync(
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
                db.WorkAssignments.AsNoTracking()
                    .Where(a => a.FaultReportId == faultReportId)
                    .OrderByDescending(a => a.AssignedAt),
                now)
            .ToListAsync(ct);
    }

    public async Task<AssignmentDetailDto> AssignAsync(AssignmentSaveDto dto, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var report = await db.FaultReports
            .Include(r => r.WorkAssignments)
            .FirstOrDefaultAsync(r => r.Id == dto.FaultReportId, ct)
            ?? throw AppException.NotFound("Prijava nije pronađena.");

        if (report.FaultStatusId == FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Zatvorena prijava se ne može mijenjati.");

        if (report.FaultTypeId is null || report.FaultPriorityId is null)
            throw AppException.Validation("Prijava mora imati određenu vrstu i prioritet prije dodjele.");

        if (report.WorkAssignments.Any(a => a.IsActive))
            throw AppException.Conflict("Prijava već ima aktivnu dodjelu. Koristite re-dodjelu.");

        await EnsureIsActiveTechnicianAsync(dto.TechnicianUserId, ct);

        var assignment = new WorkAssignment
        {
            FaultReportId = report.Id,
            TechnicianUserId = dto.TechnicianUserId,
            AssignedByUserId = currentUser.UserId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true,
            Note = dto.Note
        };
        db.WorkAssignments.Add(assignment);

        if (report.ReviewedAt is null)
        {
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedByUserId = currentUser.UserId;
        }
        ChangeStatus(report, FaultStatusIds.Dodijeljeno);

        history.Add(report.Id, HistoryChangeType.Assigned,
            oldValue: null, newValue: dto.TechnicianUserId.ToString(), note: dto.Note);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetDetailAsync(assignment.Id, ct);
    }

    public async Task<AssignmentDetailDto> ReassignAsync(
        int assignmentId, ReassignDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 5)
            throw AppException.Validation("Razlog re-dodjele je obavezan i mora imati barem 5 znakova.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var current = await db.WorkAssignments
            .Include(a => a.FaultReport)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive, ct)
            ?? throw AppException.NotFound("Aktivna dodjela nije pronađena.");

        if (current.FaultReport.FaultStatusId == FaultStatusIds.Zatvoreno)
            throw AppException.Conflict("Zatvorena prijava se ne može re-dodijeliti.");

        if (current.TechnicianUserId == dto.NewTechnicianUserId)
            throw AppException.Validation("Nalog je već dodijeljen odabranom izvršitelju.");

        await EnsureIsActiveTechnicianAsync(dto.NewTechnicianUserId, ct);

        var now = DateTime.UtcNow;
        const string interruptMessage = "Prekinuto — re-dodjela";

        var running = await db.Interventions
            .Where(i => i.WorkAssignmentId == current.Id
                        && i.InterventionStatusId == InterventionStatusIds.UTijeku)
            .ToListAsync(ct);

        InterruptInterventions(running, now, interruptMessage);

        current.IsActive = false;
        current.UnassignedAt = now;
        current.ReassignReason = dto.Reason.Trim();

        var replacement = new WorkAssignment
        {
            FaultReportId = current.FaultReportId,
            TechnicianUserId = dto.NewTechnicianUserId,
            AssignedByUserId = currentUser.UserId,
            AssignedAt = now,
            IsActive = true,
            ReassignReason = dto.Reason.Trim(),
            Note = dto.Note
        };
        db.WorkAssignments.Add(replacement);

        history.Add(current.FaultReportId, HistoryChangeType.Reassigned,
            oldValue: current.TechnicianUserId.ToString(),
            newValue: dto.NewTechnicianUserId.ToString(),
            note: dto.Reason);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetDetailAsync(replacement.Id, ct);
    }

    public async Task<TransferResultDto> TransferAssignmentsAsync(
        int fromUserId, TransferAssignmentsDto dto, CancellationToken ct = default)
    {
        if (fromUserId == dto.ReplacementTechnicianId)
            throw AppException.Validation("Zamjenik ne može biti isti izvršitelj.");

        await EnsureIsActiveTechnicianAsync(dto.ReplacementTechnicianId, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var assignments = await db.WorkAssignments
            .Where(a => a.TechnicianUserId == fromUserId && a.IsActive
                        && a.FaultReport.FaultStatusId != FaultStatusIds.Zatvoreno)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        const string reason = "Deaktivacija izvršitelja";
        const string interruptMessage = "Prekinuto — deaktivacija izvršitelja";
        var interrupted = 0;

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var runningByAssignmentId = assignmentIds.Count == 0
            ? new Dictionary<int, List<Intervention>>()
            : (await db.Interventions
                .Where(i => assignmentIds.Contains(i.WorkAssignmentId)
                            && i.InterventionStatusId == InterventionStatusIds.UTijeku)
                .ToListAsync(ct))
            .GroupBy(i => i.WorkAssignmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var assignment in assignments)
        {
            if (runningByAssignmentId.TryGetValue(assignment.Id, out var running))
                interrupted += InterruptInterventions(running, now, interruptMessage);

            assignment.IsActive = false;
            assignment.UnassignedAt = now;
            assignment.ReassignReason = reason;

            db.WorkAssignments.Add(new WorkAssignment
            {
                FaultReportId = assignment.FaultReportId,
                TechnicianUserId = dto.ReplacementTechnicianId,
                AssignedByUserId = currentUser.UserId,
                AssignedAt = now,
                IsActive = true,
                ReassignReason = reason,
                Note = dto.Note
            });

            history.Add(assignment.FaultReportId, HistoryChangeType.Reassigned,
                assignment.TechnicianUserId.ToString(),
                dto.ReplacementTechnicianId.ToString(), reason);
        }

        var user = await db.Users.FindAsync([fromUserId], ct)
            ?? throw AppException.NotFound("Korisnik nije pronađen.");
        user.IsActive = false;
        user.DeactivatedAt = now;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new TransferResultDto
        {
            TransferredCount = assignments.Count,
            InterruptedInterventionsCount = interrupted
        };
    }

    private async Task<AssignmentDetailDto> GetDetailAsync(int id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        return await db.WorkAssignments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssignmentDetailDto
            {
                Id = a.Id,
                FaultReportId = a.FaultReportId,
                TechnicianUserId = a.TechnicianUserId,
                AssignedByUserId = a.AssignedByUserId,
                AssignedAt = a.AssignedAt,
                IsActive = a.IsActive,
                UnassignedAt = a.UnassignedAt,
                ReassignReason = a.ReassignReason,
                Note = a.Note,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                ReportNumber = a.FaultReport.ReportNumber,
                Title = a.FaultReport.Title,
                LocationName = a.FaultReport.Location.Name,
                PriorityName = a.FaultReport.FaultPriority != null ? a.FaultReport.FaultPriority.Name : null,
                PriorityColor = a.FaultReport.FaultPriority != null ? a.FaultReport.FaultPriority.ColorHex : null,
                FaultStatusId = a.FaultReport.FaultStatusId,
                StatusName = a.FaultReport.FaultStatus.Name,
                DueDate = a.FaultReport.DueDate,
                IsOverdue = a.FaultReport.DueDate != null && a.FaultReport.DueDate < now
                            && a.FaultReport.FaultStatusId != FaultStatusIds.Rijeseno
                            && a.FaultReport.FaultStatusId != FaultStatusIds.Zatvoreno,
                TechnicianName = a.Technician.FirstName + " " + a.Technician.LastName,
                AssignedByName = a.AssignedByUser.FirstName + " " + a.AssignedByUser.LastName,
                InterventionCount = a.Interventions.Count
            })
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(AssignmentNotFoundMessage);
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

    private static int InterruptInterventions(
        IEnumerable<Intervention> interventions, DateTime now, string message)
    {
        var count = 0;
        foreach (var intervention in interventions)
        {
            intervention.InterventionStatusId = InterventionStatusIds.Neuspjesna;
            intervention.FinishedAt = now;
            intervention.FailureReason = message;
            intervention.Note = string.IsNullOrWhiteSpace(intervention.Note)
                ? message
                : intervention.Note + "\n" + message;
            count++;
        }

        return count;
    }

    private async Task EnsureIsActiveTechnicianAsync(int userId, CancellationToken ct)
    {
        var isActiveTechnician = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId
                && u.IsActive
                && u.UserRoles.Any(ur => ur.Role.Code == Roles.Technician), ct);

        if (!isActiveTechnician)
            throw AppException.Validation("Dodijeliti se smije samo aktivnom korisniku s ulogom izvršitelja.");
    }

    private void EnsureCanView(int technicianUserId)
    {
        if (currentUser.IsAdminOrManager)
            return;

        if (technicianUserId == currentUser.UserId)
            return;

        throw AppException.Forbidden("Nemate ovlasti za pregled ovog naloga.");
    }

    private static IQueryable<AssignmentListDto> ProjectList(IQueryable<WorkAssignment> query, DateTime now)
        => query.Select(a => new AssignmentListDto
        {
            Id = a.Id,
            FaultReportId = a.FaultReportId,
            ReportNumber = a.FaultReport.ReportNumber,
            Title = a.FaultReport.Title,
            LocationName = a.FaultReport.Location.Name,
            PriorityName = a.FaultReport.FaultPriority != null ? a.FaultReport.FaultPriority.Name : null,
            PriorityColor = a.FaultReport.FaultPriority != null ? a.FaultReport.FaultPriority.ColorHex : null,
            TechnicianName = a.Technician.FirstName + " " + a.Technician.LastName,
            AssignedAt = a.AssignedAt,
            IsActive = a.IsActive,
            UnassignedAt = a.UnassignedAt,
            ReassignReason = a.ReassignReason,
            InterventionCount = a.Interventions.Count,
            StatusName = a.FaultReport.FaultStatus.Name,
            DueDate = a.FaultReport.DueDate,
            IsOverdue = a.FaultReport.DueDate != null && a.FaultReport.DueDate < now
                        && a.FaultReport.FaultStatusId != FaultStatusIds.Rijeseno
                        && a.FaultReport.FaultStatusId != FaultStatusIds.Zatvoreno
        });
}
