namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a part category for grouping part families.
/// Note: This is distinct from the PartCategory enum in PartType.cs
/// </summary>
public class PartCategoryRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}
