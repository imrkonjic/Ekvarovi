namespace EKvarovi.Shared.Dtos.Dashboard;

public sealed class DashboardCountDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
