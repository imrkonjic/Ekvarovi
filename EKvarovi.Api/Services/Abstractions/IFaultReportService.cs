using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.FaultReports;

namespace EKvarovi.Api.Services.Abstractions;

public interface IFaultReportService
{
    Task<PagedResult<FaultReportListDto>> SearchAsync(FaultReportFilterDto filter, CancellationToken ct = default);
    Task<PagedResult<FaultReportListDto>> GetMineAsync(FaultReportFilterDto filter, CancellationToken ct = default);
    Task<FaultReportDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FaultReportDetailDto> CreateAsync(FaultReportCreateDto dto, CancellationToken ct = default);
    Task<FaultReportDetailDto> UpdateAsync(int id, FaultReportUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<FaultReportDetailDto> TriageAsync(int id, FaultReportTriageDto dto, CancellationToken ct = default);
    Task<FaultReportDetailDto> CloseAsync(int id, CloseFaultReportDto dto, CancellationToken ct = default);
    Task<FaultReportDetailDto> ReopenAsync(int id, ReopenFaultReportDto dto, CancellationToken ct = default);
}
