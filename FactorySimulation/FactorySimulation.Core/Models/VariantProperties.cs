namespace FactorySimulation.Core.Models;

/// <summary>
/// Physical properties for a specific part variant
/// </summary>
public class VariantProperties
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public double? LengthMm { get; set; }
    public double? WidthMm { get; set; }
    public double? HeightMm { get; set; }
    public double? WeightKg { get; set; }
    public string? ContainerType { get; set; }
    public int? UnitsPerContainer { get; set; }
    public bool RequiresForklift { get; set; }
    public string? Notes { get; set; }
}
