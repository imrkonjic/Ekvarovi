namespace EKvarovi.Api.Entities.Lookups;

public class InterventionStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
