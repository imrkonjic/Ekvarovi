namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class AssignmentDetailDto
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public int TechnicianUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string? ReassignReason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public int FaultStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string AssignedByName { get; set; } = string.Empty;
    public int InterventionCount { get; set; }
}
