using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Lookups;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController(ILookupService lookupService) : ControllerBase
{
    [HttpGet("all")]
    public Task<AllLookupsDto> GetAll(CancellationToken ct)
        => lookupService.GetAllAsync(ct);

    [HttpGet("location-types")]
    public Task<List<LookupDto>> GetLocationTypes(CancellationToken ct)
        => lookupService.GetLocationTypesAsync(ct);

    [HttpGet("fault-types")]
    public Task<List<LookupDto>> GetFaultTypes(CancellationToken ct)
        => lookupService.GetFaultTypesAsync(ct);

    [HttpGet("fault-priorities")]
    public Task<List<PriorityLookupDto>> GetFaultPriorities(CancellationToken ct)
        => lookupService.GetFaultPrioritiesAsync(ct);

    [HttpGet("fault-statuses")]
    public Task<List<LookupDto>> GetFaultStatuses(CancellationToken ct)
        => lookupService.GetFaultStatusesAsync(ct);

    [HttpGet("intervention-statuses")]
    public Task<List<LookupDto>> GetInterventionStatuses(CancellationToken ct)
        => lookupService.GetInterventionStatusesAsync(ct);

    [HttpGet("material-units")]
    public Task<List<LookupDto>> GetMaterialUnits(CancellationToken ct)
        => lookupService.GetMaterialUnitsAsync(ct);

    [HttpGet("roles")]
    [Authorize(Roles = Roles.Admin)]
    public Task<List<LookupDto>> GetRoles(CancellationToken ct)
        => lookupService.GetRolesAsync(ct);

    [HttpGet("technicians")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public Task<List<LookupDto>> GetTechnicians([FromQuery] string? search, CancellationToken ct)
        => lookupService.GetTechniciansAsync(search, ct);
}
