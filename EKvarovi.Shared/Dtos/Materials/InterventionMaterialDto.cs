namespace EKvarovi.Shared.Dtos.Materials;

public sealed class InterventionMaterialDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}
