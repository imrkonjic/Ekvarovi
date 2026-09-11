using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.FaultReports;

public sealed class FaultReportFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? LocationId { get; set; }
    public int? FaultTypeId { get; set; }
    public int? FaultPriorityId { get; set; }
    public int? FaultStatusId { get; set; }
    public int? TechnicianUserId { get; set; }
    public DateTime? ReportedFrom { get; set; }
    public DateTime? ReportedTo { get; set; }
    public bool? OnlyOverdue { get; set; }
}
