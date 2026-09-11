using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Users;
using EKvarovi.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<UserListDto>> Search([FromQuery] UserFilterDto filter, CancellationToken ct)
        => userService.SearchAsync(filter, ct);

    [HttpGet("{id:int}")]
    public Task<UserDetailDto> GetById(int id, CancellationToken ct)
        => userService.GetByIdAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> Create([FromBody] UserSaveDto dto, CancellationToken ct)
    {
        var result = await userService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public Task<UserDetailDto> Update(int id, [FromBody] UserSaveDto dto, CancellationToken ct)
        => userService.UpdateAsync(id, dto, ct);

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await userService.ResetPasswordAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        await userService.ActivateAsync(id, ct);
        return NoContent();
    }
}
