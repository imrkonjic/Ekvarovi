namespace EKvarovi.Shared.Dtos.Dashboard;

public sealed class DashboardDto
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public string ScopeLabel { get; set; } = string.Empty;

    public int TotalReportCount { get; set; }
    public int OverdueCount { get; set; }

    public List<DashboardCountDto> ReportsByStatus { get; set; } = [];
    public List<DashboardCountDto> OpenReportsByPriority { get; set; } = [];
    public List<DashboardTopLocationDto> TopLocations { get; set; } = [];
    public List<DashboardCountDto> ReportsByFaultType { get; set; } = [];
}
