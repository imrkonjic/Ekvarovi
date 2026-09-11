namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class ReassignDto
{
    public int NewTechnicianUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}
