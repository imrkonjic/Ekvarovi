namespace EKvarovi.Shared.Dtos.Auth;

public sealed class CurrentUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public int? HomeLocationId { get; set; }
    public string? HomeLocationName { get; set; }
}
