using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Lookups;

namespace EKvarovi.Api.Services.Abstractions;

public interface ILookupService
{
    Task<List<LookupDto>> GetLocationTypesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetFaultTypesAsync(CancellationToken ct = default);
    Task<List<PriorityLookupDto>> GetFaultPrioritiesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetFaultStatusesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetInterventionStatusesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetMaterialUnitsAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetRolesAsync(CancellationToken ct = default);
    Task<AllLookupsDto> GetAllAsync(CancellationToken ct = default);
}
