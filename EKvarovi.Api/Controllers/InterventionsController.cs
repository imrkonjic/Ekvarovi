using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Dtos.Interventions;
using EKvarovi.Shared.Dtos.Materials;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterventionsController(
    IInterventionService interventionService,
    IInterventionMaterialService interventionMaterialService,
    IAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public Task<PagedResult<InterventionListDto>> Search(
        [FromQuery] InterventionFilterDto filter, CancellationToken ct)
        => interventionService.SearchAsync(filter, ct);

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Technician)]
    public Task<PagedResult<InterventionListDto>> GetMine(
        [FromQuery] InterventionFilterDto filter, CancellationToken ct)
        => interventionService.GetMineAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<InterventionDetailDto> GetById(int id, CancellationToken ct)
        => interventionService.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public async Task<ActionResult<InterventionDetailDto>> Create(
        [FromBody] InterventionCreateDto dto, CancellationToken ct)
    {
        var result = await interventionService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public Task<InterventionDetailDto> Update(
        int id, [FromBody] InterventionUpdateDto dto, CancellationToken ct)
        => interventionService.UpdateAsync(id, dto, ct);

    [HttpPut("{id:int}/finish")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public Task<InterventionDetailDto> Finish(
        int id, [FromBody] InterventionFinishDto dto, CancellationToken ct)
        => interventionService.FinishAsync(id, dto, ct);

    [HttpGet("{id:int}/materials")]
    public Task<List<InterventionMaterialDto>> GetMaterials(int id, CancellationToken ct)
        => interventionMaterialService.GetByInterventionIdAsync(id, ct);

    [HttpPost("{id:int}/materials")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public async Task<ActionResult<InterventionMaterialDto>> AddMaterial(
        int id, [FromBody] InterventionMaterialSaveDto dto, CancellationToken ct)
    {
        var result = await interventionMaterialService.AddAsync(id, dto, ct);
        return Created($"api/interventions/{id}/materials/{result.Id}", result);
    }

    [HttpPut("{id:int}/materials/{itemId:int}")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public Task<InterventionMaterialDto> UpdateMaterial(
        int id, int itemId, [FromBody] InterventionMaterialSaveDto dto, CancellationToken ct)
        => interventionMaterialService.UpdateAsync(id, itemId, dto, ct);

    [HttpDelete("{id:int}/materials/{itemId:int}")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public async Task<IActionResult> DeleteMaterial(int id, int itemId, CancellationToken ct)
    {
        await interventionMaterialService.DeleteAsync(id, itemId, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = $"{Roles.Technician},{Roles.Admin}")]
    public async Task<ActionResult<AttachmentDto>> UploadAttachment(
        int id, [FromForm] IFormFile file, CancellationToken ct)
    {
        var result = await attachmentService.UploadToInterventionAsync(id, file, ct);
        return Created($"api/attachments/{result.Id}", result);
    }
}
