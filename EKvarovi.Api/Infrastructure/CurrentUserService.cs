using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Infrastructure;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int? UserIdOrNull =>
        int.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public int UserId => UserIdOrNull
        ?? throw AppException.Unauthorized("Korisnik nije prijavljen.");

    public bool IsInRole(string roleCode) => User?.IsInRole(roleCode) ?? false;

    public bool IsAdminOrManager => IsInRole(Roles.Admin) || IsInRole(Roles.Manager);
}
