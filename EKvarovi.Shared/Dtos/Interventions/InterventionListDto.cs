namespace EKvarovi.Shared.Dtos.Interventions;

public sealed class InterventionListDto
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public int WorkAssignmentId { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public int InterventionStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Note { get; set; }
}
