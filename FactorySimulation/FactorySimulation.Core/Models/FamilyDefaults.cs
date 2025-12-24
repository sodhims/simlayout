namespace FactorySimulation.Core.Models;

/// <summary>
/// Default physical properties for a part family (inherited by variants)
/// </summary>
public class FamilyDefaults
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public double? LengthMm { get; set; }
    public double? WidthMm { get; set; }
    public double? HeightMm { get; set; }
    public double? WeightKg { get; set; }
    public string? ContainerType { get; set; }
    public int? UnitsPerContainer { get; set; }
    public bool RequiresForklift { get; set; }
    public string? Notes { get; set; }
}
