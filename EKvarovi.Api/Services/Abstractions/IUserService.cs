using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Users;

namespace EKvarovi.Api.Services.Abstractions;

public interface IUserService
{
    Task<PagedResult<UserListDto>> SearchAsync(UserFilterDto filter, CancellationToken ct = default);
    Task<UserDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserDetailDto> CreateAsync(UserSaveDto dto, CancellationToken ct = default);
    Task<UserDetailDto> UpdateAsync(int id, UserSaveDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}
