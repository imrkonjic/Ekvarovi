using EKvarovi.Api.Data;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Lookups;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class LookupService(AppDbContext db, ICurrentUserService currentUser) : ILookupService
{
    public Task<List<LookupDto>> GetLocationTypesAsync(CancellationToken ct = default)
        => db.LocationTypes
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

    public Task<List<LookupDto>> GetFaultTypesAsync(CancellationToken ct = default)
        => db.FaultTypes
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

    public Task<List<PriorityLookupDto>> GetFaultPrioritiesAsync(CancellationToken ct = default)
        => db.FaultPriorities
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new PriorityLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                DefaultResolutionHours = x.DefaultResolutionHours,
                ColorHex = x.ColorHex
            })
            .ToListAsync(ct);

    public Task<List<LookupDto>> GetFaultStatusesAsync(CancellationToken ct = default)
        => db.FaultStatuses
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

    public Task<List<LookupDto>> GetInterventionStatusesAsync(CancellationToken ct = default)
        => db.InterventionStatuses
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

    public Task<List<LookupDto>> GetMaterialUnitsAsync(CancellationToken ct = default)
        => db.MaterialUnits
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

    public Task<List<LookupDto>> GetRolesAsync(CancellationToken ct = default)
        => db.Roles
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = true,
                SortOrder = x.Id
            })
            .ToListAsync(ct);

    public async Task<AllLookupsDto> GetAllAsync(CancellationToken ct = default)
    {
        return new AllLookupsDto
        {
            LocationTypes = await GetLocationTypesAsync(ct),
            FaultTypes = await GetFaultTypesAsync(ct),
            FaultPriorities = await GetFaultPrioritiesAsync(ct),
            FaultStatuses = await GetFaultStatusesAsync(ct),
            InterventionStatuses = await GetInterventionStatusesAsync(ct),
            MaterialUnits = await GetMaterialUnitsAsync(ct),
            Roles = currentUser.IsInRole(Roles.Admin)
                ? await GetRolesAsync(ct)
                : []
        };
    }
}
