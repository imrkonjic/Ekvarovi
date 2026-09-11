using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Materials;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class MaterialService(AppDbContext db) : IMaterialService
{
    private const string NotFoundMessage = "Materijal nije pronađen.";
    private const string DuplicateCodeMessage = "Materijal s tom šifrom već postoji.";
    private const string InUseMessage =
        "Materijal upotrijebljen na intervenciji ne može se obrisati. Deaktivirajte ga umjesto brisanja.";

    private static readonly Dictionary<string, Expression<Func<Material, object>>> SortMap = new()
    {
        ["code"] = m => m.Code,
        ["name"] = m => m.Name,
        ["materialUnit"] = m => m.MaterialUnit.Name,
        ["unitPrice"] = m => m.UnitPrice,
        ["isActive"] = m => m.IsActive
    };

    public async Task<PagedResult<MaterialListDto>> SearchAsync(
        MaterialFilterDto filter, CancellationToken ct = default)
    {
        var query = db.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(m =>
                m.Code.Contains(term) ||
                m.Name.Contains(term));
        }

        if (filter.MaterialUnitId is int materialUnitId)
            query = query.Where(m => m.MaterialUnitId == materialUnitId);

        if (filter.IsActive is bool isActive)
            query = query.Where(m => m.IsActive == isActive);

        query = query.ApplySort(filter.SortBy, filter.SortDir, SortMap, "name");

        return await query
            .Select(m => new MaterialListDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                MaterialUnitId = m.MaterialUnitId,
                MaterialUnitName = m.MaterialUnit.Name,
                UnitPrice = m.UnitPrice,
                IsActive = m.IsActive
            })
            .ToPagedResultAsync(filter, ct);
    }

    public async Task<MaterialDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await ProjectDetail(db.Materials.AsNoTracking().Where(m => m.Id == id))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(NotFoundMessage);

    public async Task<MaterialDetailDto> CreateAsync(MaterialSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, ct);

        if (await db.Materials.AnyAsync(m => m.Code == dto.Code.Trim(), ct))
            throw AppException.Conflict(DuplicateCodeMessage);

        var material = MapToEntity(new Material(), dto);
        db.Materials.Add(material);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(material.Id, ct);
    }

    public async Task<MaterialDetailDto> UpdateAsync(int id, MaterialSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, ct);

        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        var code = dto.Code.Trim();
        if (await db.Materials.AnyAsync(m => m.Id != id && m.Code == code, ct))
            throw AppException.Conflict(DuplicateCodeMessage);

        MapToEntity(material, dto);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        if (await db.InterventionMaterials.AnyAsync(im => im.MaterialId == id, ct))
            throw AppException.Conflict(InUseMessage);

        db.Materials.Remove(material);
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<MaterialDetailDto> ProjectDetail(IQueryable<Material> query)
        => query.Select(m => new MaterialDetailDto
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            MaterialUnitId = m.MaterialUnitId,
            MaterialUnitName = m.MaterialUnit.Name,
            UnitPrice = m.UnitPrice,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        });

    private async Task ValidateSaveDtoAsync(MaterialSaveDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Code))
            errors["code"] = ["Šifra je obavezna."];

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["name"] = ["Naziv je obavezan."];

        if (dto.MaterialUnitId <= 0)
            errors["materialUnitId"] = ["Mjerna jedinica je obavezna."];

        if (dto.UnitPrice < 0)
            errors["unitPrice"] = ["Jedinična cijena ne smije biti negativna."];

        if (errors.Count > 0)
            throw AppException.Validation("Neispravni podaci materijala.", errors);

        var unitExists = await db.MaterialUnits
            .AnyAsync(u => u.Id == dto.MaterialUnitId && u.IsActive, ct);

        if (!unitExists)
            throw AppException.Validation("Odabrana mjerna jedinica ne postoji ili nije aktivna.");
    }

    private static Material MapToEntity(Material material, MaterialSaveDto dto)
    {
        material.Code = dto.Code.Trim();
        material.Name = dto.Name.Trim();
        material.MaterialUnitId = dto.MaterialUnitId;
        material.UnitPrice = dto.UnitPrice;
        material.IsActive = dto.IsActive;
        return material;
    }
}
