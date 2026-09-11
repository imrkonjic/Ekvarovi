using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKvarovi.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get([FromQuery] DashboardFilterDto filter, CancellationToken ct)
        => dashboardService.GetAsync(filter, ct);
}
