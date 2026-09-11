using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;

namespace EKvarovi.Api.Entities;

public class Material : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaterialUnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public MaterialUnit MaterialUnit { get; set; } = null!;
    public ICollection<InterventionMaterial> InterventionMaterials { get; set; } = new List<InterventionMaterial>();
}
