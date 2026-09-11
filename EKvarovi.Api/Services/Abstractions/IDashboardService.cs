using EKvarovi.Shared.Dtos.Dashboard;

namespace EKvarovi.Api.Services.Abstractions;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(DashboardFilterDto filter, CancellationToken ct = default);
}
