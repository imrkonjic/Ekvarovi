using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Assignments;

namespace EKvarovi.Api.Services.Abstractions;

public interface IWorkAssignmentService
{
    Task<PagedResult<AssignmentListDto>> SearchAsync(AssignmentFilterDto filter, CancellationToken ct = default);
    Task<PagedResult<AssignmentListDto>> GetMineAsync(AssignmentFilterDto filter, CancellationToken ct = default);
    Task<AssignmentDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentListDto>> GetByFaultReportIdAsync(int faultReportId, CancellationToken ct = default);
    Task<AssignmentDetailDto> AssignAsync(AssignmentSaveDto dto, CancellationToken ct = default);
    Task<AssignmentDetailDto> ReassignAsync(int assignmentId, ReassignDto dto, CancellationToken ct = default);
    Task<TransferResultDto> TransferAssignmentsAsync(
        int fromUserId, TransferAssignmentsDto dto, CancellationToken ct = default);
}
