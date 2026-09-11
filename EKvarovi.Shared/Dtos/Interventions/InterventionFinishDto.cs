namespace EKvarovi.Shared.Dtos.Interventions;

public sealed class InterventionFinishDto
{
    public bool IsSuccessful { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Note { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
