using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthResponseDto> Login([FromBody] LoginDto dto, CancellationToken ct)
        => authService.LoginAsync(dto, ct);

    [HttpGet("me")]
    [Authorize]
    public Task<CurrentUserDto> GetMe(CancellationToken ct)
        => authService.GetCurrentUserAsync(ct);
}
