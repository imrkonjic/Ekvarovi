namespace EKvarovi.Shared.Dtos.Auth;

public sealed class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public CurrentUserDto User { get; set; } = new();
}
