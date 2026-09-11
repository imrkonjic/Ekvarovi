using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Assignments;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkAssignmentsController(IWorkAssignmentService workAssignmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public Task<PagedResult<AssignmentListDto>> Search(
        [FromQuery] AssignmentFilterDto filter, CancellationToken ct)
        => workAssignmentService.SearchAsync(filter, ct);

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Technician)]
    public Task<PagedResult<AssignmentListDto>> GetMine(
        [FromQuery] AssignmentFilterDto filter, CancellationToken ct)
        => workAssignmentService.GetMineAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<AssignmentDetailDto> GetById(int id, CancellationToken ct)
        => workAssignmentService.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Manager},{Roles.Admin}")]
    public async Task<ActionResult<AssignmentDetailDto>> Assign(
        [FromBody] AssignmentSaveDto dto, CancellationToken ct)
    {
        var result = await workAssignmentService.AssignAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}/reassign")]
    [Authorize(Roles = $"{Roles.Manager},{Roles.Admin}")]
    public Task<AssignmentDetailDto> Reassign(
        int id, [FromBody] ReassignDto dto, CancellationToken ct)
        => workAssignmentService.ReassignAsync(id, dto, ct);
}
