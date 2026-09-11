using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Users;

public sealed class UserFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public int? LocationId { get; set; }
    public bool? IsActive { get; set; }
}
