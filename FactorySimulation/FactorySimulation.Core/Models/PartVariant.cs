namespace FactorySimulation.Core.Models;

/// <summary>
/// Represents a specific variant of a part within a family
/// </summary>
public class PartVariant
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Display property for family code (populated by repository)
    /// </summary>
    public string? FamilyCode { get; set; }

    /// <summary>
    /// Display property for family name (populated by repository)
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Inherited from family (computed, not stored)
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Display property for category name (inherited from family)
    /// </summary>
    public string? CategoryName { get; set; }
}
