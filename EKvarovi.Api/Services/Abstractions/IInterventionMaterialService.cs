using EKvarovi.Shared.Dtos.Materials;

namespace EKvarovi.Api.Services.Abstractions;

public interface IInterventionMaterialService
{
    Task<List<InterventionMaterialDto>> GetByInterventionIdAsync(
        int interventionId, CancellationToken ct = default);

    Task<InterventionMaterialDto> AddAsync(
        int interventionId, InterventionMaterialSaveDto dto, CancellationToken ct = default);

    Task<InterventionMaterialDto> UpdateAsync(
        int interventionId, int itemId, InterventionMaterialSaveDto dto, CancellationToken ct = default);

    Task DeleteAsync(int interventionId, int itemId, CancellationToken ct = default);
}
