using EKvarovi.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await attachmentService.OpenForDownloadAsync(id, ct);
        return File(result.Stream, result.ContentType, result.OriginalFileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await attachmentService.DeleteAsync(id, ct);
        return NoContent();
    }
}
