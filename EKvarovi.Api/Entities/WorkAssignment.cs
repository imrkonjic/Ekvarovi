using EKvarovi.Api.Entities.Common;

namespace EKvarovi.Api.Entities;

public class WorkAssignment : AuditableEntity
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public int TechnicianUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UnassignedAt { get; set; }
    public string? ReassignReason { get; set; }
    public string? Note { get; set; }

    public FaultReport FaultReport { get; set; } = null!;
    public User Technician { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
}
