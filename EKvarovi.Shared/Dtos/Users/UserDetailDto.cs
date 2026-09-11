namespace EKvarovi.Shared.Dtos.Users;

public sealed class UserDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Specialization { get; set; }
    public int? HomeLocationId { get; set; }
    public string? HomeLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public IReadOnlyList<int> RoleIds { get; set; } = [];
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
