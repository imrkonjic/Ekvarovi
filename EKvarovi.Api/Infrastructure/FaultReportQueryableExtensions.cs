using EKvarovi.Api.Entities;
using EKvarovi.Api.Services.Abstractions;

namespace EKvarovi.Api.Infrastructure;

public static class FaultReportQueryableExtensions
{
    public static IQueryable<FaultReport> VisibleTo(
        this IQueryable<FaultReport> query, ICurrentUserService user)
    {
        if (user.IsAdminOrManager)
            return query;

        var id = user.UserId;

        return query.Where(r =>
            r.ReportedByUserId == id ||
            r.WorkAssignments.Any(a => a.TechnicianUserId == id));
    }
}
