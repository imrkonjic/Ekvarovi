using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Locations;

namespace EKvarovi.Api.Services.Abstractions;

public interface ILocationService
{
    Task<PagedResult<LocationListDto>> SearchAsync(LocationFilterDto filter, CancellationToken ct = default);
    Task<LocationDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<LocationDetailDto> CreateAsync(LocationSaveDto dto, CancellationToken ct = default);
    Task<LocationDetailDto> UpdateAsync(int id, LocationSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
