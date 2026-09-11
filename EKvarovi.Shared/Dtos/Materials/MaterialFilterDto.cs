using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Materials;

public sealed class MaterialFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? MaterialUnitId { get; set; }
    public bool? IsActive { get; set; }
}
