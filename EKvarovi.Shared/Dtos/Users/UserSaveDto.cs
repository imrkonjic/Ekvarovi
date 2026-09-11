namespace EKvarovi.Shared.Dtos.Users;

public sealed class UserSaveDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? Specialization { get; set; }
    public int? HomeLocationId { get; set; }
    public List<int> RoleIds { get; set; } = [];
}
