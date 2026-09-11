using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Interventions;

namespace EKvarovi.Api.Services.Abstractions;

public interface IInterventionService
{
    Task<PagedResult<InterventionListDto>> SearchAsync(
        InterventionFilterDto filter, CancellationToken ct = default);
    Task<PagedResult<InterventionListDto>> GetMineAsync(
        InterventionFilterDto filter, CancellationToken ct = default);
    Task<InterventionDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<InterventionDetailDto> CreateAsync(InterventionCreateDto dto, CancellationToken ct = default);
    Task<InterventionDetailDto> UpdateAsync(int id, InterventionUpdateDto dto, CancellationToken ct = default);
    Task<InterventionDetailDto> FinishAsync(int id, InterventionFinishDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<InterventionListDto>> GetByFaultReportIdAsync(
        int faultReportId, CancellationToken ct = default);
}
