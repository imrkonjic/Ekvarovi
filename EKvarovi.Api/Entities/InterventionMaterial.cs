namespace EKvarovi.Api.Entities;

public class InterventionMaterial
{
    public int Id { get; set; }
    public int InterventionId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    public Intervention Intervention { get; set; } = null!;
    public Material Material { get; set; } = null!;
}
