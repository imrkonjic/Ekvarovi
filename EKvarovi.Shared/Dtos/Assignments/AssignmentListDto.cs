namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class AssignmentListDto
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string? ReassignReason { get; set; }
    public int InterventionCount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
}
