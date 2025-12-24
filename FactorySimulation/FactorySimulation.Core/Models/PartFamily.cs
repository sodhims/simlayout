namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a family of related parts with common characteristics
/// </summary>
public class PartFamily
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string FamilyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Display property for category name (populated by repository)
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Navigation property for variants in this family
    /// </summary>
    public List<PartVariant> Variants { get; set; } = new();
}
