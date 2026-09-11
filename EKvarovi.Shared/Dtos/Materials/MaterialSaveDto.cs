namespace EKvarovi.Shared.Dtos.Materials;

public sealed class MaterialSaveDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaterialUnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
