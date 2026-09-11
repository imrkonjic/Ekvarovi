using EKvarovi.Shared.Dtos.Attachments;

namespace EKvarovi.Shared.Dtos.FaultReports;

public sealed class FaultReportDetailDto
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int? FaultTypeId { get; set; }
    public string? FaultTypeName { get; set; }
    public int? FaultPriorityId { get; set; }
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public int FaultStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int ReportedByUserId { get; set; }
    public string ReportedByName { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosingNote { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public List<AttachmentDto> PhotosBefore { get; set; } = [];
    public List<AttachmentDto> Documents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
