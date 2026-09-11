namespace EKvarovi.Api.Services.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }
    int? UserIdOrNull { get; }
    bool IsInRole(string roleCode);
    bool IsAdminOrManager { get; }
}
