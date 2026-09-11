namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class AssignmentSaveDto
{
    public int FaultReportId { get; set; }
    public int TechnicianUserId { get; set; }
    public string? Note { get; set; }
}
