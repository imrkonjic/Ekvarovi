namespace EKvarovi.Shared.Dtos.FaultReports;

public sealed class FaultReportTriageDto
{
    public int FaultTypeId { get; set; }
    public int FaultPriorityId { get; set; }
    public DateTime? DueDate { get; set; }
}
