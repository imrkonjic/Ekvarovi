using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;

namespace EKvarovi.Api.Entities;

public class Location : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public int LocationTypeId { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    public LocationType LocationType { get; set; } = null!;
    public ICollection<User> HomeUsers { get; set; } = new List<User>();
}
