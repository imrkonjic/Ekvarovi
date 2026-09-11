using EKvarovi.Shared.Dtos.Auth;

namespace EKvarovi.Api.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken ct = default);
}
