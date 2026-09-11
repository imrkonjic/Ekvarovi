using EKvarovi.Api.Entities;

namespace EKvarovi.Api.Services.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(User user, IReadOnlyList<string> roleCodes);
}
