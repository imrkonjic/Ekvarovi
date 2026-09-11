using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Assignments;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Dtos.FaultReports;
using EKvarovi.Shared.Dtos.Interventions;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FaultReportsController(
    IFaultReportService faultReportService,
    IWorkAssignmentService workAssignmentService,
    IInterventionService interventionService,
    IAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public Task<PagedResult<FaultReportListDto>> Search(
        [FromQuery] FaultReportFilterDto filter, CancellationToken ct)
        => faultReportService.SearchAsync(filter, ct);

    [HttpGet("mine")]
    public Task<PagedResult<FaultReportListDto>> GetMine(
        [FromQuery] FaultReportFilterDto filter, CancellationToken ct)
        => faultReportService.GetMineAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<FaultReportDetailDto> GetById(int id, CancellationToken ct)
        => faultReportService.GetByIdAsync(id, ct);

    [HttpGet("{id:int}/assignments")]
    public Task<IReadOnlyList<AssignmentListDto>> GetAssignments(int id, CancellationToken ct)
        => workAssignmentService.GetByFaultReportIdAsync(id, ct);

    [HttpGet("{id:int}/interventions")]
    public Task<IReadOnlyList<InterventionListDto>> GetInterventions(int id, CancellationToken ct)
        => interventionService.GetByFaultReportIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Reporter},{Roles.Manager},{Roles.Admin}")]
    public async Task<ActionResult<FaultReportDetailDto>> Create(
        [FromBody] FaultReportCreateDto dto, CancellationToken ct)
    {
        var result = await faultReportService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Reporter},{Roles.Manager},{Roles.Admin}")]
    public Task<FaultReportDetailDto> Update(
        int id, [FromBody] FaultReportUpdateDto dto, CancellationToken ct)
        => faultReportService.UpdateAsync(id, dto, ct);

    [HttpPut("{id:int}/triage")]
    [Authorize(Roles = $"{Roles.Manager},{Roles.Admin}")]
    public Task<FaultReportDetailDto> Triage(
        int id, [FromBody] FaultReportTriageDto dto, CancellationToken ct)
        => faultReportService.TriageAsync(id, dto, ct);

    [HttpPut("{id:int}/close")]
    [Authorize(Roles = $"{Roles.Manager},{Roles.Admin}")]
    public Task<FaultReportDetailDto> Close(
        int id, [FromBody] CloseFaultReportDto dto, CancellationToken ct)
        => faultReportService.CloseAsync(id, dto, ct);

    [HttpPut("{id:int}/reopen")]
    [Authorize(Roles = $"{Roles.Manager},{Roles.Admin}")]
    public Task<FaultReportDetailDto> Reopen(
        int id, [FromBody] ReopenFaultReportDto dto, CancellationToken ct)
        => faultReportService.ReopenAsync(id, dto, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.Reporter},{Roles.Admin}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await faultReportService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = $"{Roles.Reporter},{Roles.Manager},{Roles.Admin}")]
    public async Task<ActionResult<AttachmentDto>> UploadAttachment(
        int id,
        [FromForm] IFormFile file,
        [FromForm] AttachmentPurpose purpose,
        CancellationToken ct)
    {
        var result = await attachmentService.UploadToFaultReportAsync(id, purpose, file, ct);
        return Created($"api/attachments/{result.Id}", result);
    }
}
