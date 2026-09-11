using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Entities;

public class FaultReportHistory
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public HistoryChangeType ChangeType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }

    public FaultReport FaultReport { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
