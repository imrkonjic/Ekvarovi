using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Interventions;

public sealed class InterventionFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? InterventionStatusId { get; set; }
    public int? TechnicianUserId { get; set; }
    public DateTime? StartedFrom { get; set; }
    public DateTime? StartedTo { get; set; }
}
