namespace EKvarovi.Shared.Dtos.FaultReports;

public sealed class FaultReportCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationId { get; set; }
}
