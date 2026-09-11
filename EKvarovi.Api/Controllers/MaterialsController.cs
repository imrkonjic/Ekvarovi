using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Materials;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaterialsController(IMaterialService materialService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<MaterialListDto>> Search([FromQuery] MaterialFilterDto filter, CancellationToken ct)
        => materialService.SearchAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<MaterialDetailDto> GetById(int id, CancellationToken ct)
        => materialService.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MaterialDetailDto>> Create([FromBody] MaterialSaveDto dto, CancellationToken ct)
    {
        var result = await materialService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<MaterialDetailDto> Update(int id, [FromBody] MaterialSaveDto dto, CancellationToken ct)
        => materialService.UpdateAsync(id, dto, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await materialService.DeleteAsync(id, ct);
        return NoContent();
    }
}
