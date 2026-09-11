using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;

namespace EKvarovi.Api.Entities;

public class Intervention : AuditableEntity
{
    public int Id { get; set; }
    public int WorkAssignmentId { get; set; }
    public int FaultReportId { get; set; }
    public int TechnicianUserId { get; set; }
    public int InterventionStatusId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Note { get; set; }
    public string? FailureReason { get; set; }

    public WorkAssignment WorkAssignment { get; set; } = null!;
    public FaultReport FaultReport { get; set; } = null!;
    public User Technician { get; set; } = null!;
    public InterventionStatus InterventionStatus { get; set; } = null!;
}
