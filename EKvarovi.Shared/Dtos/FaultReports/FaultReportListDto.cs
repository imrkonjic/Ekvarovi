namespace EKvarovi.Shared.Dtos.FaultReports;

public sealed class FaultReportListDto
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? FaultTypeName { get; set; }
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime ReportedAt { get; set; }
}
