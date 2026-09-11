using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Locations;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class LocationService(AppDbContext db) : ILocationService
{
    private const string NotFoundMessage = "Lokacija nije pronađena.";
    private const string DuplicateCodeMessage = "Lokacija s tom šifrom već postoji.";
    private const string HasFaultReportsMessage =
        "Lokacija s postojećim prijavama ne može se obrisati. Deaktivirajte je umjesto brisanja.";

    private static readonly Dictionary<string, Expression<Func<Location, object>>> SortMap = new()
    {
        ["code"] = l => l.Code,
        ["name"] = l => l.Name,
        ["city"] = l => l.City,
        ["locationType"] = l => l.LocationType.Name,
        ["isActive"] = l => l.IsActive
    };

    public async Task<PagedResult<LocationListDto>> SearchAsync(
        LocationFilterDto filter, CancellationToken ct = default)
    {
        var query = db.Locations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(l =>
                l.Code.Contains(term) ||
                l.Name.Contains(term) ||
                l.Address.Contains(term) ||
                l.City.Contains(term) ||
                (l.ContactPerson != null && l.ContactPerson.Contains(term)) ||
                (l.ContactPhone != null && l.ContactPhone.Contains(term)));
        }

        if (filter.LocationTypeId is int locationTypeId)
            query = query.Where(l => l.LocationTypeId == locationTypeId);

        if (filter.IsActive is bool isActive)
            query = query.Where(l => l.IsActive == isActive);

        query = query.ApplySort(filter.SortBy, filter.SortDir, SortMap, "name");

        return await query
            .Select(l => new LocationListDto
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name,
                Address = l.Address,
                City = l.City,
                LocationTypeId = l.LocationTypeId,
                LocationTypeName = l.LocationType.Name,
                IsActive = l.IsActive
            })
            .ToPagedResultAsync(filter, ct);
    }

    public async Task<LocationDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await ProjectDetail(db.Locations.AsNoTracking().Where(l => l.Id == id))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(NotFoundMessage);

    public async Task<LocationDetailDto> CreateAsync(LocationSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, ct);

        if (await db.Locations.AnyAsync(l => l.Code == dto.Code.Trim(), ct))
            throw AppException.Conflict(DuplicateCodeMessage);

        var location = MapToEntity(new Location(), dto);
        db.Locations.Add(location);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(location.Id, ct);
    }

    public async Task<LocationDetailDto> UpdateAsync(int id, LocationSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, ct);

        var location = await db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        var code = dto.Code.Trim();
        if (await db.Locations.AnyAsync(l => l.Id != id && l.Code == code, ct))
            throw AppException.Conflict(DuplicateCodeMessage);

        MapToEntity(location, dto);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var location = await db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        if (await db.FaultReports.AnyAsync(r => r.LocationId == id, ct))
            throw AppException.Conflict(HasFaultReportsMessage);

        db.Locations.Remove(location);
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<LocationDetailDto> ProjectDetail(IQueryable<Location> query)
        => query.Select(l => new LocationDetailDto
        {
            Id = l.Id,
            Code = l.Code,
            Name = l.Name,
            Address = l.Address,
            City = l.City,
            PostalCode = l.PostalCode,
            LocationTypeId = l.LocationTypeId,
            LocationTypeName = l.LocationType.Name,
            ContactPerson = l.ContactPerson,
            ContactPhone = l.ContactPhone,
            IsActive = l.IsActive,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });

    private async Task ValidateSaveDtoAsync(LocationSaveDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Code))
            errors["code"] = ["Šifra je obavezna."];

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["name"] = ["Naziv je obavezan."];

        if (string.IsNullOrWhiteSpace(dto.Address))
            errors["address"] = ["Adresa je obavezna."];

        if (string.IsNullOrWhiteSpace(dto.City))
            errors["city"] = ["Grad je obavezan."];

        if (dto.LocationTypeId <= 0)
            errors["locationTypeId"] = ["Vrsta lokacije je obavezna."];

        if (errors.Count > 0)
            throw AppException.Validation("Neispravni podaci lokacije.", errors);

        var typeExists = await db.LocationTypes
            .AnyAsync(t => t.Id == dto.LocationTypeId && t.IsActive, ct);

        if (!typeExists)
            throw AppException.Validation("Odabrana vrsta lokacije ne postoji ili nije aktivna.");
    }

    private static Location MapToEntity(Location location, LocationSaveDto dto)
    {
        location.Code = dto.Code.Trim();
        location.Name = dto.Name.Trim();
        location.Address = dto.Address.Trim();
        location.City = dto.City.Trim();
        location.PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();
        location.LocationTypeId = dto.LocationTypeId;
        location.ContactPerson = string.IsNullOrWhiteSpace(dto.ContactPerson) ? null : dto.ContactPerson.Trim();
        location.ContactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone) ? null : dto.ContactPhone.Trim();
        location.IsActive = dto.IsActive;
        return location;
    }
}
