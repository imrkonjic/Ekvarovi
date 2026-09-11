using EKvarovi.Api.Data;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Dtos.Dashboard;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class DashboardService(
    AppDbContext db,
    ICurrentUserService currentUser) : IDashboardService
{
    private const int DefaultPeriodDays = 30;

    public async Task<DashboardDto> GetAsync(DashboardFilterDto filter, CancellationToken ct = default)
    {
        var (periodFrom, periodTo) = ResolvePeriod(filter);
        var now = DateTime.UtcNow;

        var baseQuery = db.FaultReports
            .AsNoTracking()
            .VisibleTo(currentUser)
            .Where(r => r.ReportedAt >= periodFrom && r.ReportedAt <= periodTo);

        var totalReportCount = await baseQuery.CountAsync(ct);

        var overdueCount = await baseQuery
            .Where(r => r.DueDate != null
                        && r.DueDate < now
                        && r.FaultStatusId != FaultStatusIds.Rijeseno
                        && r.FaultStatusId != FaultStatusIds.Zatvoreno)
            .CountAsync(ct);

        var reportsByStatus = await baseQuery
            .GroupBy(r => r.FaultStatusId)
            .Select(g => new DashboardCountDto
            {
                Id = g.Key,
                Name = g.Select(r => r.FaultStatus.Name).First(),
                Count = g.Count()
            })
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        var openReportsByPriority = await baseQuery
            .Where(r => r.FaultStatusId != FaultStatusIds.Zatvoreno)
            .GroupBy(r => r.FaultPriorityId)
            .Select(g => new DashboardCountDto
            {
                Id = g.Key,
                Name = g.Select(r => r.FaultPriority != null ? r.FaultPriority.Name : "Nekategorizirano").First(),
                Count = g.Count()
            })
            .OrderBy(x => x.Id ?? int.MaxValue)
            .ToListAsync(ct);

        var topLocations = await baseQuery
            .GroupBy(r => new { r.LocationId, r.Location.Name })
            .Select(g => new DashboardTopLocationDto
            {
                LocationId = g.Key.LocationId,
                LocationName = g.Key.Name,
                ReportCount = g.Count()
            })
            .OrderByDescending(x => x.ReportCount)
            .ThenBy(x => x.LocationName)
            .Take(5)
            .ToListAsync(ct);

        var reportsByFaultType = await baseQuery
            .GroupBy(r => r.FaultTypeId)
            .Select(g => new DashboardCountDto
            {
                Id = g.Key,
                Name = g.Select(r => r.FaultType != null ? r.FaultType.Name : "Nekategorizirano").First(),
                Count = g.Count()
            })
            .OrderBy(x => x.Id ?? int.MaxValue)
            .ToListAsync(ct);

        return new DashboardDto
        {
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            ScopeLabel = ResolveScopeLabel(),
            TotalReportCount = totalReportCount,
            OverdueCount = overdueCount,
            ReportsByStatus = reportsByStatus,
            OpenReportsByPriority = openReportsByPriority,
            TopLocations = topLocations,
            ReportsByFaultType = reportsByFaultType
        };
    }

    private static (DateTime From, DateTime To) ResolvePeriod(DashboardFilterDto filter)
    {
        if (filter.PeriodFrom is null && filter.PeriodTo is null)
        {
            var periodEnd = DateTime.UtcNow;
            return (periodEnd.AddDays(-DefaultPeriodDays), periodEnd);
        }

        var periodFrom = DateTimeNormalizer.ToUtc(filter.PeriodFrom)
                         ?? DateTimeNormalizer.ToUtc(filter.PeriodTo)!.Value.AddDays(-DefaultPeriodDays);
        var periodTo = DateTimeNormalizer.ToUtc(filter.PeriodTo) ?? DateTime.UtcNow;

        if (periodFrom > periodTo)
            throw AppException.Validation("Početni datum ne smije biti nakon završnog.");

        return (periodFrom, periodTo);
    }

    private string ResolveScopeLabel()
    {
        if (currentUser.IsAdminOrManager)
            return "Sve prijave";

        if (currentUser.IsInRole(Roles.Technician) && !currentUser.IsInRole(Roles.Reporter))
            return "Moji nalozi";

        if (currentUser.IsInRole(Roles.Reporter) && !currentUser.IsInRole(Roles.Technician))
            return "Moje prijave";

        return "Osobni opseg";
    }
}
