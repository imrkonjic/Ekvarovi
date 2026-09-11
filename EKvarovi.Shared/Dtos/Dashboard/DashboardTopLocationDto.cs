namespace EKvarovi.Shared.Dtos.Dashboard;

public sealed class DashboardTopLocationDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int ReportCount { get; set; }
}
