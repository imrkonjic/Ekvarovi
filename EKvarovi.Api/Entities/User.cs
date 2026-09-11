using EKvarovi.Api.Entities.Common;

namespace EKvarovi.Api.Entities;

public class User : AuditableEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Specialization { get; set; }
    public int? HomeLocationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }

    public Location? HomeLocation { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
