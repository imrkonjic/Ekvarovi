using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Assignments;

public sealed class AssignmentFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? TechnicianUserId { get; set; }
    public bool? IsActive { get; set; }
    public int? LocationId { get; set; }
    public DateTime? AssignedFrom { get; set; }
    public DateTime? AssignedTo { get; set; }
    public bool? OnlyActive { get; set; }
}
