using EKvarovi.Shared.Dtos.Attachments;

namespace EKvarovi.Shared.Dtos.Interventions;

public sealed class InterventionDetailDto
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int FaultStatusId { get; set; }
    public string FaultStatusName { get; set; } = string.Empty;
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public List<AttachmentDto> PhotosAfter { get; set; } = [];
}
