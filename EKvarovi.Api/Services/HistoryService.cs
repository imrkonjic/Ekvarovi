using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Services;

public sealed class HistoryService(AppDbContext db, ICurrentUserService currentUser) : IHistoryService
{
    public void Add(int faultReportId, HistoryChangeType changeType, string? oldValue, string? newValue, string? note)
    {
        db.FaultReportHistories.Add(new FaultReportHistory
        {
            FaultReportId = faultReportId,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            Note = note,
            ChangedByUserId = currentUser.UserId,
            ChangedAt = DateTime.UtcNow
        });
    }
}
