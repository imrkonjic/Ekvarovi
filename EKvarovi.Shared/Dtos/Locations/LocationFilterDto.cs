using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Locations;

public sealed class LocationFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? LocationTypeId { get; set; }
    public bool? IsActive { get; set; }
}
