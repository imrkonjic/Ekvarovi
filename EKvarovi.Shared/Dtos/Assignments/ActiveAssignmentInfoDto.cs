namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class ActiveAssignmentInfoDto
{
    public int AssignmentId { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
}
