namespace EKvarovi.Api.Entities.Lookups;

public class FaultStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsClosedState { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
