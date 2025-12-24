using FactorySimulation.Core.Models;

namespace FactorySimulation.Services;

/// <summary>
/// Service for importing and exporting parts and BOMs
/// </summary>
public interface IPartImportExportService
{
    /// <summary>
    /// Exports all parts and their BOMs to a JSON string
    /// </summary>
    Task<string> ExportToJsonAsync();

    /// <summary>
    /// Exports all parts and their BOMs to a file
    /// </summary>
    Task ExportToFileAsync(string filePath);

    /// <summary>
    /// Imports parts and BOMs from a JSON string
    /// </summary>
    /// <returns>Tuple of (success, error message, count of imported parts)</returns>
    Task<(bool Success, string? Error, int ImportedCount)> ImportFromJsonAsync(string json);

    /// <summary>
    /// Imports parts and BOMs from a file
    /// </summary>
    Task<(bool Success, string? Error, int ImportedCount)> ImportFromFileAsync(string filePath);

    /// <summary>
    /// Imports parts and BOMs from a folder containing product JSON files
    /// </summary>
    Task<(bool Success, string? Error, int ImportedCount)> ImportFromFolderAsync(string folderPath);

    /// <summary>
    /// Validates import data without actually importing
    /// </summary>
    Task<(bool IsValid, List<string> Errors, List<string> Warnings)> ValidateImportAsync(string json);

    /// <summary>
    /// Detects the format of a JSON string
    /// </summary>
    ImportFormat DetectFormat(string json);

    /// <summary>
    /// Builds a preview of the import without actually importing
    /// </summary>
    /// <returns>Preview data for display in dialog</returns>
    Task<ImportPreviewData?> BuildImportPreviewAsync(string json);

    /// <summary>
    /// Imports a product with potentially modified quantities
    /// </summary>
    Task<(bool Success, string? Error, int ImportedCount)> ImportWithModificationsAsync(
        string json,
        Dictionary<string, decimal> modifiedQuantities);
}

/// <summary>
/// Data for the import preview dialog
/// </summary>
public class ImportPreviewData
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public List<ImportPreviewNode> RootNodes { get; set; } = new();
    public int TotalParts { get; set; }
    public int NewParts { get; set; }
    public int ExistingParts { get; set; }
}

/// <summary>
/// Node in the import preview tree
/// </summary>
public class ImportPreviewNode
{
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public string UnitOfMeasure { get; set; } = "EA";
    public bool IsNew { get; set; }
    public bool IsRoot { get; set; }
    public List<ImportPreviewNode> Children { get; set; } = new();
}

/// <summary>
/// Import format types
/// </summary>
public enum ImportFormat
{
    /// <summary>Flat list format with parts array</summary>
    FlatList,
    /// <summary>Nested BOM format with product/bom structure</summary>
    NestedBom,
    /// <summary>Components list format with components array</summary>
    ComponentsList,
    /// <summary>Subassemblies list format with subassemblies array</summary>
    SubassembliesList,
    /// <summary>Unknown format</summary>
    Unknown
}
