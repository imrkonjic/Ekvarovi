using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Materials;

namespace EKvarovi.Api.Services.Abstractions;

public interface IMaterialService
{
    Task<PagedResult<MaterialListDto>> SearchAsync(MaterialFilterDto filter, CancellationToken ct = default);
    Task<MaterialDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MaterialDetailDto> CreateAsync(MaterialSaveDto dto, CancellationToken ct = default);
    Task<MaterialDetailDto> UpdateAsync(int id, MaterialSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
