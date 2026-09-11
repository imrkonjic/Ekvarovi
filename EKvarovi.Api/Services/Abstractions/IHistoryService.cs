using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Services.Abstractions;

public interface IHistoryService
{
    void Add(int faultReportId, HistoryChangeType changeType, string? oldValue, string? newValue, string? note);
}
