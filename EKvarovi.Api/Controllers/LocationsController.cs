using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Locations;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<LocationListDto>> Search([FromQuery] LocationFilterDto filter, CancellationToken ct)
        => locationService.SearchAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<LocationDetailDto> GetById(int id, CancellationToken ct)
        => locationService.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<LocationDetailDto>> Create([FromBody] LocationSaveDto dto, CancellationToken ct)
    {
        var result = await locationService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<LocationDetailDto> Update(int id, [FromBody] LocationSaveDto dto, CancellationToken ct)
        => locationService.UpdateAsync(id, dto, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await locationService.DeleteAsync(id, ct);
        return NoContent();
    }
}
