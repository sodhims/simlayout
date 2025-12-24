using System.Text.Json.Serialization;

namespace FactorySimulation.Core.Models;

/// <summary>
/// Root object for importing/exporting parts and BOMs
/// </summary>
public class PartImportExport
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("parts")]
    public List<PartExportItem> Parts { get; set; } = new();
}

/// <summary>
/// A part with its BOM for import/export
/// </summary>
public class PartExportItem
{
    [JsonPropertyName("partNumber")]
    public string PartNumber { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Component";

    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; } = "EA";

    [JsonPropertyName("unitCost")]
    public decimal UnitCost { get; set; }

    [JsonPropertyName("bom")]
    public List<BomExportItem>? Bom { get; set; }
}

/// <summary>
/// A BOM line item for import/export
/// </summary>
public class BomExportItem
{
    [JsonPropertyName("componentPartNumber")]
    public string ComponentPartNumber { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public double Quantity { get; set; } = 1;

    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; } = "EA";

    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
