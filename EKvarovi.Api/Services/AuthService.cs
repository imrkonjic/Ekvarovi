using EKvarovi.Api.Data;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Infrastructure.Options;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EKvarovi.Api.Services;

public sealed class AuthService(
    AppDbContext db,
    IJwtTokenService jwtTokenService,
    ICurrentUserService currentUser,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const string InvalidCredentialsMessage = "Neispravni podaci za prijavu.";
    private const string InactiveUserMessage = "Korisnički račun nije aktivan.";

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.HomeLocation)
            .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw AppException.Unauthorized(InvalidCredentialsMessage);

        if (!user.IsActive)
            throw AppException.Unauthorized(InactiveUserMessage);

        var roleCodes = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var token = jwtTokenService.GenerateToken(user, roleCodes);
        var expiresAt = DateTime.UtcNow.AddHours(jwtOptions.Value.ExpiryHours);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToCurrentUserDto(user, roleCodes)
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;

        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.HomeLocation)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("Korisnik nije pronađen.");

        if (!user.IsActive)
            throw AppException.Unauthorized(InactiveUserMessage);

        var roleCodes = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        return MapToCurrentUserDto(user, roleCodes);
    }

    private static CurrentUserDto MapToCurrentUserDto(Entities.User user, IReadOnlyList<string> roleCodes) =>
        new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = roleCodes,
            HomeLocationId = user.HomeLocationId,
            HomeLocationName = user.HomeLocation?.Name
        };
}
