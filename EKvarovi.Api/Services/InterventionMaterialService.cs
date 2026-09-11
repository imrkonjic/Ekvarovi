using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Dtos.Materials;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class InterventionMaterialService(
    AppDbContext db,
    ICurrentUserService currentUser) : IInterventionMaterialService
{
    private const string InterventionNotFoundMessage = "Intervencija nije pronađena.";
    private const string ItemNotFoundMessage = "Stavka materijala nije pronađena.";
    private const string QuantityValidationMessage =
        "Količina mora biti strogo veća od nule i najviše 99999,99.";
    private const string AddToFinishedMessage =
        "Materijal se može dodati samo na intervenciju koja nije završena.";
    private const string ModifyFinishedMessage =
        "Završena intervencija se više ne smije uređivati ni brisati.";
    private const string InactiveMaterialMessage =
        "Dodati se smije samo aktivan materijal iz kataloga.";
    private const string DuplicateMaterialMessage =
        "Isti materijal ne smije se pojaviti dvaput na istoj intervenciji.";
    private const string ForbiddenMessage =
        "Materijal smije mijenjati samo vlasnik intervencije.";
    private const string ViewForbiddenMessage =
        "Nemate ovlasti za pregled ove intervencije.";

    public async Task<List<InterventionMaterialDto>> GetByInterventionIdAsync(
        int interventionId, CancellationToken ct = default)
    {
        await EnsureInterventionVisibleAsync(interventionId, ct);

        return await db.InterventionMaterials
            .AsNoTracking()
            .Where(im => im.InterventionId == interventionId)
            .OrderBy(im => im.CreatedAt)
            .Select(im => new InterventionMaterialDto
            {
                Id = im.Id,
                MaterialId = im.MaterialId,
                MaterialCode = im.Material.Code,
                MaterialName = im.Material.Name,
                MaterialUnitName = im.Material.MaterialUnit.Name,
                Quantity = im.Quantity,
                UnitPriceSnapshot = im.UnitPriceSnapshot,
                LineTotal = im.Quantity * im.UnitPriceSnapshot,
                CreatedAt = im.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<InterventionMaterialDto> AddAsync(
        int interventionId, InterventionMaterialSaveDto dto, CancellationToken ct = default)
    {
        ValidateQuantity(dto.Quantity);

        var intervention = await db.Interventions
            .Include(i => i.WorkAssignment)
            .FirstOrDefaultAsync(i => i.Id == interventionId, ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        EnsureCanModifyMaterial(intervention);
        EnsureInterventionNotFinished(intervention, AddToFinishedMessage);

        var material = await LoadActiveMaterialAsync(dto.MaterialId, ct);

        if (await db.InterventionMaterials.AnyAsync(
            im => im.InterventionId == interventionId && im.MaterialId == dto.MaterialId, ct))
            throw AppException.Conflict(DuplicateMaterialMessage);

        var item = new InterventionMaterial
        {
            InterventionId = interventionId,
            MaterialId = dto.MaterialId,
            Quantity = dto.Quantity,
            UnitPriceSnapshot = material.UnitPrice,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUser.UserId
        };

        db.InterventionMaterials.Add(item);
        await db.SaveChangesAsync(ct);

        return MapToDto(item, material);
    }

    public async Task<InterventionMaterialDto> UpdateAsync(
        int interventionId, int itemId, InterventionMaterialSaveDto dto, CancellationToken ct = default)
    {
        ValidateQuantity(dto.Quantity);

        var item = await LoadItemAsync(interventionId, itemId, ct);

        EnsureCanModifyMaterial(item.Intervention);
        EnsureInterventionNotFinished(item.Intervention, ModifyFinishedMessage);

        Material material;
        if (dto.MaterialId != item.MaterialId)
        {
            if (await db.InterventionMaterials.AnyAsync(
                im => im.InterventionId == interventionId
                      && im.MaterialId == dto.MaterialId
                      && im.Id != itemId, ct))
                throw AppException.Conflict(DuplicateMaterialMessage);

            material = await LoadActiveMaterialAsync(dto.MaterialId, ct);
            item.MaterialId = dto.MaterialId;
            item.UnitPriceSnapshot = material.UnitPrice;
        }
        else
        {
            material = item.Material;
        }

        item.Quantity = dto.Quantity;
        await db.SaveChangesAsync(ct);

        return MapToDto(item, material);
    }

    public async Task DeleteAsync(int interventionId, int itemId, CancellationToken ct = default)
    {
        var item = await LoadItemAsync(interventionId, itemId, ct);

        EnsureCanModifyMaterial(item.Intervention);
        EnsureInterventionNotFinished(item.Intervention, ModifyFinishedMessage);

        db.InterventionMaterials.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private async Task<InterventionMaterial> LoadItemAsync(
        int interventionId, int itemId, CancellationToken ct)
        => await db.InterventionMaterials
            .Include(im => im.Intervention)
            .ThenInclude(i => i.WorkAssignment)
            .Include(im => im.Material)
            .ThenInclude(m => m.MaterialUnit)
            .FirstOrDefaultAsync(im => im.Id == itemId && im.InterventionId == interventionId, ct)
            ?? throw AppException.NotFound(ItemNotFoundMessage);

    private async Task<Material> LoadActiveMaterialAsync(int materialId, CancellationToken ct)
    {
        var material = await db.Materials
            .Include(m => m.MaterialUnit)
            .FirstOrDefaultAsync(m => m.Id == materialId, ct);

        if (material is null)
            throw AppException.Validation("Materijal nije pronađen.");

        if (!material.IsActive)
            throw AppException.Validation(InactiveMaterialMessage);

        return material;
    }

    private async Task EnsureInterventionVisibleAsync(int interventionId, CancellationToken ct)
    {
        var faultReportId = await db.Interventions
            .AsNoTracking()
            .Where(i => i.Id == interventionId)
            .Select(i => (int?)i.FaultReportId)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(InterventionNotFoundMessage);

        var visible = await db.FaultReports
            .AsNoTracking()
            .VisibleTo(currentUser)
            .AnyAsync(r => r.Id == faultReportId, ct);

        if (!visible)
            throw AppException.Forbidden(ViewForbiddenMessage);
    }

    private void EnsureCanModifyMaterial(Intervention intervention)
    {
        if (currentUser.IsInRole(Roles.Admin))
            return;

        if (intervention.WorkAssignment.TechnicianUserId != currentUser.UserId
            || !intervention.WorkAssignment.IsActive)
            throw AppException.Forbidden(ForbiddenMessage);
    }

    private static void EnsureInterventionNotFinished(Intervention intervention, string message)
    {
        if (intervention.InterventionStatusId is InterventionStatusIds.Zavrsena
                                              or InterventionStatusIds.Neuspjesna)
            throw AppException.Conflict(message);
    }

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0 || quantity > 99999.99m)
            throw AppException.Validation(QuantityValidationMessage);
    }

    private static InterventionMaterialDto MapToDto(InterventionMaterial item, Material material)
        => new()
        {
            Id = item.Id,
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            MaterialUnitName = material.MaterialUnit.Name,
            Quantity = item.Quantity,
            UnitPriceSnapshot = item.UnitPriceSnapshot,
            LineTotal = item.Quantity * item.UnitPriceSnapshot,
            CreatedAt = item.CreatedAt
        };
}
