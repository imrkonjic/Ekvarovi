namespace EKvarovi.Shared.Dtos.Users;

public sealed class UserListDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? HomeLocationId { get; set; }
    public string? HomeLocationName { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
