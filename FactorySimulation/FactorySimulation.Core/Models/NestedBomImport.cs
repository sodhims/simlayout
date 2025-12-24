using System.Text.Json.Serialization;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Root object for importing nested BOM JSON format (single product file)
/// </summary>
public class NestedProductImport
{
    [JsonPropertyName("product")]
    public ProductInfo? Product { get; set; }

    [JsonPropertyName("bom")]
    public NestedBomNode? Bom { get; set; }
}

/// <summary>
/// Product information
/// </summary>
public class ProductInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("root_subassembly")]
    public string? RootSubassembly { get; set; }

    [JsonPropertyName("revision")]
    public string? Revision { get; set; }

    [JsonPropertyName("created_utc")]
    public DateTime? CreatedUtc { get; set; }

    [JsonPropertyName("base_product_id")]
    public string? BaseProductId { get; set; }

    [JsonPropertyName("variant_of_revision")]
    public string? VariantOfRevision { get; set; }
}

/// <summary>
/// Product variant import format with BOM overrides
/// </summary>
public class ProductVariantImport
{
    [JsonPropertyName("product")]
    public ProductInfo? Product { get; set; }

    [JsonPropertyName("bom_overrides")]
    public List<BomOverride>? BomOverrides { get; set; }
}

/// <summary>
/// BOM override operation for product variants
/// </summary>
public class BomOverride
{
    [JsonPropertyName("op")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("parent_subassembly_id")]
    public string ParentSubassemblyId { get; set; } = string.Empty;

    [JsonPropertyName("child_type")]
    public string? ChildType { get; set; }

    [JsonPropertyName("child_id")]
    public string? ChildId { get; set; }

    [JsonPropertyName("qty")]
    public double? Qty { get; set; }

    [JsonPropertyName("delta")]
    public double? Delta { get; set; }

    [JsonPropertyName("old_child_type")]
    public string? OldChildType { get; set; }

    [JsonPropertyName("old_child_id")]
    public string? OldChildId { get; set; }

    [JsonPropertyName("new_child_type")]
    public string? NewChildType { get; set; }

    [JsonPropertyName("new_child_id")]
    public string? NewChildId { get; set; }

    [JsonPropertyName("new_qty")]
    public double? NewQty { get; set; }
}

/// <summary>
/// A node in the nested BOM structure
/// </summary>
public class NestedBomNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("children")]
    public List<NestedBomChild>? Children { get; set; }
}

/// <summary>
/// A child reference in the nested BOM
/// </summary>
public class NestedBomChild
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("qty")]
    public double Qty { get; set; } = 1;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("children")]
    public List<NestedBomChild>? Children { get; set; }
}

/// <summary>
/// Multi-product import format (folder with multiple product files)
/// </summary>
public class MultiProductImport
{
    public List<NestedProductImport> Products { get; set; } = new();

    /// <summary>
    /// Subassembly definitions referenced by products
    /// </summary>
    public Dictionary<string, SubassemblyDefinition> Subassemblies { get; set; } = new();

    /// <summary>
    /// Component definitions
    /// </summary>
    public Dictionary<string, ComponentDefinition> Components { get; set; } = new();
}

/// <summary>
/// Subassembly definition from subassemblies.json
/// </summary>
public class SubassemblyDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("children")]
    public List<NestedBomChild>? Children { get; set; }
}

/// <summary>
/// Component definition from components.json
/// </summary>
public class ComponentDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uom")]
    public string UnitOfMeasure { get; set; } = "EA";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("unit_cost")]
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Wrapper for subassemblies.json file format
/// </summary>
public class SubassembliesFileWrapper
{
    [JsonPropertyName("subassemblies")]
    public List<SubassemblyDefinition>? Subassemblies { get; set; }
}

/// <summary>
/// Wrapper for components.json file format
/// </summary>
public class ComponentsFileWrapper
{
    [JsonPropertyName("components")]
    public List<ComponentDefinition>? Components { get; set; }
}
