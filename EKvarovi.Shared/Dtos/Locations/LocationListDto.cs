namespace EKvarovi.Shared.Dtos.Locations;

public sealed class LocationListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int LocationTypeId { get; set; }
    public string LocationTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
