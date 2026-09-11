using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;

namespace EKvarovi.Api.Entities;

public class FaultReport : AuditableEntity
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public int? FaultTypeId { get; set; }
    public int? FaultPriorityId { get; set; }
    public int FaultStatusId { get; set; }
    public DateTime? DueDate { get; set; }
    public int ReportedByUserId { get; set; }
    public DateTime ReportedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosingNote { get; set; }

    public Location Location { get; set; } = null!;
    public FaultType? FaultType { get; set; }
    public FaultPriority? FaultPriority { get; set; }
    public FaultStatus FaultStatus { get; set; } = null!;
    public User ReportedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public User? ClosedByUser { get; set; }
    public ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
    public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
}
