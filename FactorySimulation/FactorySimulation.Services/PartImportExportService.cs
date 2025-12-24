using System.Text.Json;
using FactorySimulation.Core.Models;
using FactorySimulation.Data.Repositories;

namespace FactorySimulation.Services;

/// <summary>
/// Service for importing and exporting parts and BOMs
/// </summary>
public class PartImportExportService : IPartImportExportService
{
    private readonly IPartTypeRepository _partTypeRepository;
    private readonly IBomRepository _bomRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PartImportExportService(
        IPartTypeRepository partTypeRepository,
        IBomRepository bomRepository)
    {
        _partTypeRepository = partTypeRepository;
        _bomRepository = bomRepository;
    }

    public ImportFormat DetectFormat(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for nested BOM format (has "product" and "bom" properties)
            if (root.TryGetProperty("product", out _) && root.TryGetProperty("bom", out _))
            {
                return ImportFormat.NestedBom;
            }

            // Check for flat list format (has "parts" array)
            if (root.TryGetProperty("parts", out _))
            {
                return ImportFormat.FlatList;
            }

            // Check for components.json format (has "components" array)
            if (root.TryGetProperty("components", out _))
            {
                return ImportFormat.ComponentsList;
            }

            // Check for subassemblies.json format (has "subassemblies" array)
            if (root.TryGetProperty("subassemblies", out _))
            {
                return ImportFormat.SubassembliesList;
            }

            return ImportFormat.Unknown;
        }
        catch
        {
            return ImportFormat.Unknown;
        }
    }

    public async Task<string> ExportToJsonAsync()
    {
        var export = new PartImportExport
        {
            Version = "1.0",
            ExportedAt = DateTime.Now
        };

        // Get all parts
        var parts = await _partTypeRepository.GetAllAsync();

        foreach (var part in parts)
        {
            var exportItem = new PartExportItem
            {
                PartNumber = part.PartNumber,
                Name = part.Name,
                Description = part.Description,
                Category = part.Category.ToString(),
                UnitOfMeasure = part.UnitOfMeasure,
                UnitCost = part.UnitCost
            };

            // Get BOM if exists
            var bom = await _bomRepository.GetByPartTypeIdAsync(part.Id);
            if (bom?.Items != null && bom.Items.Count > 0)
            {
                exportItem.Bom = new List<BomExportItem>();
                foreach (var item in bom.Items)
                {
                    exportItem.Bom.Add(new BomExportItem
                    {
                        ComponentPartNumber = item.ComponentPart?.PartNumber ?? "",
                        Quantity = (double)item.Quantity,
                        UnitOfMeasure = item.UnitOfMeasure,
                        Sequence = item.Sequence,
                        Notes = item.Notes
                    });
                }
            }

            export.Parts.Add(exportItem);
        }

        return JsonSerializer.Serialize(export, JsonOptions);
    }

    public async Task ExportToFileAsync(string filePath)
    {
        var json = await ExportToJsonAsync();
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<(bool Success, string? Error, int ImportedCount)> ImportFromJsonAsync(string json)
    {
        var format = DetectFormat(json);

        return format switch
        {
            ImportFormat.FlatList => await ImportFlatListAsync(json),
            ImportFormat.NestedBom => await ImportNestedBomAsync(json),
            ImportFormat.ComponentsList => await ImportComponentsListAsync(json),
            ImportFormat.SubassembliesList => await ImportSubassembliesListAsync(json),
            _ => (false, "Unknown JSON format. Expected either flat list format with 'parts' array, nested BOM format with 'product' and 'bom' objects, or components/subassemblies list.", 0)
        };
    }

    private async Task<(bool Success, string? Error, int ImportedCount)> ImportFlatListAsync(string json)
    {
        try
        {
            var import = JsonSerializer.Deserialize<PartImportExport>(json, JsonOptions);
            if (import == null)
            {
                return (false, "Invalid JSON format", 0);
            }

            // Validate first
            var (isValid, errors, _) = await ValidateImportAsync(json);
            if (!isValid)
            {
                return (false, string.Join("\n", errors), 0);
            }

            int importedCount = 0;
            var partIdMap = new Dictionary<string, int>(); // partNumber -> id

            // First pass: import all parts (without BOMs)
            foreach (var item in import.Parts)
            {
                var existingPart = await _partTypeRepository.GetByPartNumberAsync(item.PartNumber);

                if (existingPart != null)
                {
                    // Update existing part
                    existingPart.Name = item.Name;
                    existingPart.Description = item.Description;
                    existingPart.Category = ParseCategory(item.Category);
                    existingPart.UnitOfMeasure = item.UnitOfMeasure;
                    existingPart.UnitCost = item.UnitCost;
                    await _partTypeRepository.UpdateAsync(existingPart);
                    partIdMap[item.PartNumber] = existingPart.Id;
                }
                else
                {
                    // Create new part
                    var newPart = new PartType
                    {
                        PartNumber = item.PartNumber,
                        Name = item.Name,
                        Description = item.Description,
                        Category = ParseCategory(item.Category),
                        UnitOfMeasure = item.UnitOfMeasure,
                        UnitCost = item.UnitCost
                    };
                    var id = await _partTypeRepository.CreateAsync(newPart);
                    partIdMap[item.PartNumber] = id;
                    importedCount++;
                }
            }

            // Second pass: import BOMs
            foreach (var item in import.Parts)
            {
                if (item.Bom == null || item.Bom.Count == 0) continue;

                var parentId = partIdMap[item.PartNumber];

                // Check if BOM already exists
                var existingBom = await _bomRepository.GetByPartTypeIdAsync(parentId);
                int bomId;

                if (existingBom != null)
                {
                    bomId = existingBom.Id;
                    // Clear existing items
                    await _bomRepository.ClearItemsAsync(bomId);
                }
                else
                {
                    // Create new BOM
                    var newBom = new BillOfMaterials
                    {
                        PartTypeId = parentId,
                        Version = 1,
                        IsActive = true
                    };
                    bomId = await _bomRepository.CreateAsync(newBom);
                }

                // Add BOM items
                int sequence = 0;
                foreach (var bomItem in item.Bom)
                {
                    if (!partIdMap.TryGetValue(bomItem.ComponentPartNumber, out var componentId))
                    {
                        // Try to find by part number in database
                        var componentPart = await _partTypeRepository.GetByPartNumberAsync(bomItem.ComponentPartNumber);
                        if (componentPart == null) continue;
                        componentId = componentPart.Id;
                    }

                    var newItem = new BOMItem
                    {
                        BomId = bomId,
                        ComponentPartTypeId = componentId,
                        Quantity = (decimal)bomItem.Quantity,
                        UnitOfMeasure = bomItem.UnitOfMeasure,
                        Sequence = bomItem.Sequence > 0 ? bomItem.Sequence : sequence++,
                        Notes = bomItem.Notes
                    };
                    await _bomRepository.AddItemAsync(newItem);
                }
            }

            return (true, null, importedCount);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    private async Task<(bool Success, string? Error, int ImportedCount)> ImportNestedBomAsync(string json)
    {
        try
        {
            var import = JsonSerializer.Deserialize<NestedProductImport>(json, JsonOptions);
            if (import?.Product == null || import.Bom == null)
            {
                return (false, "Invalid nested BOM format", 0);
            }

            int importedCount = 0;
            var partIdMap = new Dictionary<string, int>(); // partNumber -> id

            // Collect all unique parts from the BOM tree
            var allParts = new Dictionary<string, (string Name, PartCategory Category, double Qty)>();
            CollectPartsFromBom(import.Bom, allParts);

            // Add the product itself
            allParts[import.Product.Id] = (import.Product.Name, PartCategory.FinishedGood, 1);

            // First pass: create all parts
            foreach (var (partNumber, (name, category, _)) in allParts)
            {
                var existingPart = await _partTypeRepository.GetByPartNumberAsync(partNumber);

                if (existingPart != null)
                {
                    partIdMap[partNumber] = existingPart.Id;
                }
                else
                {
                    var newPart = new PartType
                    {
                        PartNumber = partNumber,
                        Name = name,
                        Category = category,
                        UnitOfMeasure = "EA"
                    };
                    var id = await _partTypeRepository.CreateAsync(newPart);
                    partIdMap[partNumber] = id;
                    importedCount++;
                }
            }

            // Second pass: create BOMs from the tree structure
            await CreateBomFromNestedNode(import.Bom, partIdMap);

            // Third pass: The product should have the same BOM as the root subassembly
            // (i.e., the product's BOM directly contains all the children, not just a link to the root subassembly)
            if (import.Product.Id != import.Bom.Id &&
                partIdMap.TryGetValue(import.Product.Id, out var productId) &&
                import.Bom.Children != null && import.Bom.Children.Count > 0)
            {
                // Create the product's BOM with the same children as the root subassembly
                var existingProductBom = await _bomRepository.GetByPartTypeIdAsync(productId);
                int productBomId;

                if (existingProductBom != null)
                {
                    productBomId = existingProductBom.Id;
                    await _bomRepository.ClearItemsAsync(productBomId);
                }
                else
                {
                    var newBom = new BillOfMaterials
                    {
                        PartTypeId = productId,
                        Version = 1,
                        IsActive = true
                    };
                    productBomId = await _bomRepository.CreateAsync(newBom);
                }

                // Add the same children that the root subassembly has
                int sequence = 0;
                foreach (var child in import.Bom.Children)
                {
                    if (!partIdMap.TryGetValue(child.Id, out var componentId)) continue;

                    var bomItem = new BOMItem
                    {
                        BomId = productBomId,
                        ComponentPartTypeId = componentId,
                        Quantity = (decimal)child.Qty,
                        UnitOfMeasure = "EA",
                        Sequence = sequence++
                    };
                    await _bomRepository.AddItemAsync(bomItem);
                }
            }

            return (true, null, importedCount);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    private void CollectPartsFromBom(NestedBomNode node, Dictionary<string, (string Name, PartCategory Category, double Qty)> parts)
    {
        // Add the node itself (subassembly)
        var nodeName = node.Name ?? node.Id;
        if (!parts.ContainsKey(node.Id))
        {
            parts[node.Id] = (nodeName, PartCategory.SubAssembly, 1);
        }

        if (node.Children == null) return;

        foreach (var child in node.Children)
        {
            var childName = child.Name ?? child.Id;
            var category = child.Type?.ToLowerInvariant() switch
            {
                "component" => PartCategory.Component,
                "subassembly" => PartCategory.SubAssembly,
                "raw" or "rawmaterial" => PartCategory.RawMaterial,
                _ => PartCategory.Component
            };

            if (!parts.ContainsKey(child.Id))
            {
                parts[child.Id] = (childName, category, child.Qty);
            }

            // Recursively collect from nested children
            if (child.Children != null && child.Children.Count > 0)
            {
                var childNode = new NestedBomNode
                {
                    Id = child.Id,
                    Name = child.Name,
                    Children = child.Children
                };
                CollectPartsFromBom(childNode, parts);
            }
        }
    }

    private async Task CreateBomFromNestedNode(NestedBomNode node, Dictionary<string, int> partIdMap)
    {
        if (node.Children == null || node.Children.Count == 0) return;

        if (!partIdMap.TryGetValue(node.Id, out var parentId)) return;

        // Check if BOM already exists
        var existingBom = await _bomRepository.GetByPartTypeIdAsync(parentId);
        int bomId;

        if (existingBom != null)
        {
            bomId = existingBom.Id;
            await _bomRepository.ClearItemsAsync(bomId);
        }
        else
        {
            var newBom = new BillOfMaterials
            {
                PartTypeId = parentId,
                Version = 1,
                IsActive = true
            };
            bomId = await _bomRepository.CreateAsync(newBom);
        }

        // Add BOM items
        int sequence = 0;
        foreach (var child in node.Children)
        {
            if (!partIdMap.TryGetValue(child.Id, out var componentId)) continue;

            var newItem = new BOMItem
            {
                BomId = bomId,
                ComponentPartTypeId = componentId,
                Quantity = (decimal)child.Qty,
                UnitOfMeasure = "EA",
                Sequence = sequence++
            };
            await _bomRepository.AddItemAsync(newItem);

            // Recursively create BOMs for nested children
            if (child.Children != null && child.Children.Count > 0)
            {
                var childNode = new NestedBomNode
                {
                    Id = child.Id,
                    Name = child.Name,
                    Children = child.Children
                };
                await CreateBomFromNestedNode(childNode, partIdMap);
            }
        }
    }

    public async Task<(bool Success, string? Error, int ImportedCount)> ImportFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (false, $"File not found: {filePath}", 0);
        }

        var json = await File.ReadAllTextAsync(filePath);
        return await ImportFromJsonAsync(json);
    }

    private async Task<(bool Success, string? Error, int ImportedCount)> ImportComponentsListAsync(string json)
    {
        try
        {
            var wrapper = JsonSerializer.Deserialize<ComponentsFileWrapper>(json, JsonOptions);
            if (wrapper?.Components == null || wrapper.Components.Count == 0)
            {
                return (false, "No components found in file", 0);
            }

            int importedCount = 0;

            foreach (var comp in wrapper.Components)
            {
                var existingPart = await _partTypeRepository.GetByPartNumberAsync(comp.Id);

                if (existingPart != null)
                {
                    // Update existing
                    existingPart.Name = comp.Name;
                    existingPart.Description = comp.Description;
                    existingPart.UnitOfMeasure = comp.UnitOfMeasure;
                    existingPart.UnitCost = comp.UnitCost;
                    await _partTypeRepository.UpdateAsync(existingPart);
                }
                else
                {
                    // Create new component
                    var newPart = new PartType
                    {
                        PartNumber = comp.Id,
                        Name = comp.Name,
                        Description = comp.Description,
                        Category = PartCategory.Component,
                        UnitOfMeasure = comp.UnitOfMeasure,
                        UnitCost = comp.UnitCost
                    };
                    await _partTypeRepository.CreateAsync(newPart);
                    importedCount++;
                }
            }

            return (true, null, importedCount);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    private async Task<(bool Success, string? Error, int ImportedCount)> ImportSubassembliesListAsync(string json)
    {
        try
        {
            var wrapper = JsonSerializer.Deserialize<SubassembliesFileWrapper>(json, JsonOptions);
            if (wrapper?.Subassemblies == null || wrapper.Subassemblies.Count == 0)
            {
                return (false, "No subassemblies found in file", 0);
            }

            int importedCount = 0;
            var partIdMap = new Dictionary<string, int>();

            // Sort by level so we create lower-level subassemblies first
            var sortedSubassemblies = wrapper.Subassemblies.OrderBy(s => s.Level).ToList();

            // First pass: create all subassembly part types
            foreach (var sa in sortedSubassemblies)
            {
                var existingPart = await _partTypeRepository.GetByPartNumberAsync(sa.Id);

                if (existingPart != null)
                {
                    partIdMap[sa.Id] = existingPart.Id;
                    existingPart.Name = sa.Name;
                    await _partTypeRepository.UpdateAsync(existingPart);
                }
                else
                {
                    var newPart = new PartType
                    {
                        PartNumber = sa.Id,
                        Name = sa.Name,
                        Category = PartCategory.SubAssembly,
                        UnitOfMeasure = "EA"
                    };
                    var id = await _partTypeRepository.CreateAsync(newPart);
                    partIdMap[sa.Id] = id;
                    importedCount++;
                }
            }

            // Second pass: create BOMs for subassemblies
            foreach (var sa in sortedSubassemblies)
            {
                if (sa.Children == null || sa.Children.Count == 0) continue;
                if (!partIdMap.TryGetValue(sa.Id, out var parentId)) continue;

                // Check if BOM already exists
                var existingBom = await _bomRepository.GetByPartTypeIdAsync(parentId);
                int bomId;

                if (existingBom != null)
                {
                    bomId = existingBom.Id;
                    await _bomRepository.ClearItemsAsync(bomId);
                }
                else
                {
                    var newBom = new BillOfMaterials
                    {
                        PartTypeId = parentId,
                        Version = 1,
                        IsActive = true
                    };
                    bomId = await _bomRepository.CreateAsync(newBom);
                }

                // Add BOM items
                int sequence = 0;
                foreach (var child in sa.Children)
                {
                    // Try to find the child part
                    int componentId;
                    if (partIdMap.TryGetValue(child.Id, out componentId))
                    {
                        // Found in our map
                    }
                    else
                    {
                        // Try to find in database
                        var componentPart = await _partTypeRepository.GetByPartNumberAsync(child.Id);
                        if (componentPart == null) continue;
                        componentId = componentPart.Id;
                    }

                    var bomItem = new BOMItem
                    {
                        BomId = bomId,
                        ComponentPartTypeId = componentId,
                        Quantity = (decimal)child.Qty,
                        UnitOfMeasure = "EA",
                        Sequence = sequence++
                    };
                    await _bomRepository.AddItemAsync(bomItem);
                }
            }

            return (true, null, importedCount);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    private async Task<(bool Success, string? Error, int ImportedCount)> ImportProductVariantAsync(string json)
    {
        try
        {
            var import = JsonSerializer.Deserialize<ProductVariantImport>(json, JsonOptions);
            if (import?.Product == null)
            {
                return (false, "Invalid product variant format", 0);
            }

            int importedCount = 0;

            // Create or get the variant part
            var variantPart = await _partTypeRepository.GetByPartNumberAsync(import.Product.Id);
            if (variantPart == null)
            {
                variantPart = new PartType
                {
                    PartNumber = import.Product.Id,
                    Name = import.Product.Name,
                    Description = $"Variant of {import.Product.BaseProductId}",
                    Category = PartCategory.FinishedGood,
                    UnitOfMeasure = "EA"
                };
                variantPart.Id = await _partTypeRepository.CreateAsync(variantPart);
                importedCount++;
            }

            // Get the base product to copy its BOM
            if (!string.IsNullOrEmpty(import.Product.BaseProductId))
            {
                var baseProduct = await _partTypeRepository.GetByPartNumberAsync(import.Product.BaseProductId);
                if (baseProduct != null)
                {
                    var baseBom = await _bomRepository.GetByPartTypeIdAsync(baseProduct.Id);
                    if (baseBom?.Items != null)
                    {
                        // Build variant BOM by copying base and applying overrides
                        var variantBomItems = new Dictionary<string, (int ComponentId, decimal Qty, string UoM)>();

                        // Copy base BOM items
                        foreach (var item in baseBom.Items)
                        {
                            var componentPart = item.ComponentPart ?? await _partTypeRepository.GetByIdAsync(item.ComponentPartTypeId);
                            if (componentPart != null)
                            {
                                variantBomItems[componentPart.PartNumber] = (item.ComponentPartTypeId, item.Quantity, item.UnitOfMeasure);
                            }
                        }

                        // Apply overrides
                        if (import.BomOverrides != null)
                        {
                            foreach (var ovr in import.BomOverrides)
                            {
                                switch (ovr.Operation.ToLowerInvariant())
                                {
                                    case "qty_delta":
                                        // Adjust quantity by delta
                                        if (!string.IsNullOrEmpty(ovr.ChildId) && variantBomItems.TryGetValue(ovr.ChildId, out var existing))
                                        {
                                            var newQty = existing.Qty + (decimal)(ovr.Delta ?? 0);
                                            if (newQty > 0)
                                            {
                                                variantBomItems[ovr.ChildId] = (existing.ComponentId, newQty, existing.UoM);
                                            }
                                            else
                                            {
                                                variantBomItems.Remove(ovr.ChildId);
                                            }
                                        }
                                        break;

                                    case "add_child":
                                        // Add new child
                                        if (!string.IsNullOrEmpty(ovr.ChildId))
                                        {
                                            var childPart = await _partTypeRepository.GetByPartNumberAsync(ovr.ChildId);
                                            if (childPart != null)
                                            {
                                                variantBomItems[ovr.ChildId] = (childPart.Id, (decimal)(ovr.Qty ?? 1), "EA");
                                            }
                                        }
                                        break;

                                    case "replace_child":
                                        // Remove old, add new
                                        if (!string.IsNullOrEmpty(ovr.OldChildId))
                                        {
                                            variantBomItems.Remove(ovr.OldChildId);
                                        }
                                        if (!string.IsNullOrEmpty(ovr.NewChildId))
                                        {
                                            var newChildPart = await _partTypeRepository.GetByPartNumberAsync(ovr.NewChildId);
                                            if (newChildPart != null)
                                            {
                                                variantBomItems[ovr.NewChildId] = (newChildPart.Id, (decimal)(ovr.NewQty ?? 1), "EA");
                                            }
                                        }
                                        break;

                                    case "remove_child":
                                        if (!string.IsNullOrEmpty(ovr.ChildId))
                                        {
                                            variantBomItems.Remove(ovr.ChildId);
                                        }
                                        break;
                                }
                            }
                        }

                        // Create the variant's BOM
                        var existingVariantBom = await _bomRepository.GetByPartTypeIdAsync(variantPart.Id);
                        int variantBomId;

                        if (existingVariantBom != null)
                        {
                            variantBomId = existingVariantBom.Id;
                            await _bomRepository.ClearItemsAsync(variantBomId);
                        }
                        else
                        {
                            var newBom = new BillOfMaterials
                            {
                                PartTypeId = variantPart.Id,
                                Version = 1,
                                IsActive = true
                            };
                            variantBomId = await _bomRepository.CreateAsync(newBom);
                        }

                        // Add variant BOM items
                        int sequence = 0;
                        foreach (var (partNumber, (componentId, qty, uom)) in variantBomItems)
                        {
                            var bomItem = new BOMItem
                            {
                                BomId = variantBomId,
                                ComponentPartTypeId = componentId,
                                Quantity = qty,
                                UnitOfMeasure = uom,
                                Sequence = sequence++
                            };
                            await _bomRepository.AddItemAsync(bomItem);
                        }
                    }
                }
            }

            return (true, null, importedCount);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string? Error, int ImportedCount)> ImportFromFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return (false, $"Folder not found: {folderPath}", 0);
        }

        int totalImported = 0;
        var errors = new List<string>();
        var warnings = new List<string>();
        var partIdMap = new Dictionary<string, int>();

        var subassemblies = new Dictionary<string, SubassemblyDefinition>();
        var components = new Dictionary<string, ComponentDefinition>();
        var products = new List<(string FilePath, NestedProductImport Product)>();

        // Check for folder structure: Components/, Subassemblies/, Products/, Products_variants/
        var componentsFolder = Path.Combine(folderPath, "Components");
        var subassembliesFolder = Path.Combine(folderPath, "Subassemblies");
        var productsFolder = Path.Combine(folderPath, "Products");
        var variantsFolder = Path.Combine(folderPath, "Products_variants");

        // ========== PHASE 1: Load all data (no database writes yet) ==========

        // 1. Load components - from Components folder or components.json
        if (Directory.Exists(componentsFolder))
        {
            foreach (var file in Directory.GetFiles(componentsFolder, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var list = ParseComponentsJson(json);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var c in list)
                        {
                            components[c.Id] = c;
                        }
                    }
                    else
                    {
                        errors.Add($"Failed to parse components from {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error reading {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
        else
        {
            // Try single file
            var componentsFile = Path.Combine(folderPath, "components.json");
            if (File.Exists(componentsFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(componentsFile);
                    var list = ParseComponentsJson(json);
                    if (list != null)
                    {
                        foreach (var c in list)
                        {
                            components[c.Id] = c;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error reading components.json: {ex.Message}");
                }
            }
        }

        // 2. Load subassemblies - from Subassemblies folder or subassemblies.json
        if (Directory.Exists(subassembliesFolder))
        {
            foreach (var file in Directory.GetFiles(subassembliesFolder, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var list = ParseSubassembliesJson(json);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var sa in list)
                        {
                            subassemblies[sa.Id] = sa;
                        }
                    }
                    else
                    {
                        errors.Add($"Failed to parse subassemblies from {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error reading {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
        else
        {
            // Try single file
            var subassembliesFile = Path.Combine(folderPath, "subassemblies.json");
            if (File.Exists(subassembliesFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(subassembliesFile);
                    var list = ParseSubassembliesJson(json);
                    if (list != null)
                    {
                        foreach (var sa in list)
                        {
                            subassemblies[sa.Id] = sa;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error reading subassemblies.json: {ex.Message}");
                }
            }
        }

        // 3. Load products
        var productSearchFolder = Directory.Exists(productsFolder) ? productsFolder : folderPath;
        var productFiles = Directory.GetFiles(productSearchFolder, "*.json")
            .Where(f => !f.EndsWith("subassemblies.json", StringComparison.OrdinalIgnoreCase) &&
                       !f.EndsWith("components.json", StringComparison.OrdinalIgnoreCase));

        foreach (var file in productFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var format = DetectFormat(json);

                if (format == ImportFormat.NestedBom)
                {
                    var import = JsonSerializer.Deserialize<NestedProductImport>(json, JsonOptions);
                    if (import?.Product != null && import.Bom != null)
                    {
                        products.Add((file, import));
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // ========== PHASE 2: Validate dependencies ==========

        var allKnownParts = new HashSet<string>();
        allKnownParts.UnionWith(components.Keys);
        allKnownParts.UnionWith(subassemblies.Keys);
        foreach (var (_, prod) in products)
        {
            if (prod.Product != null)
            {
                allKnownParts.Add(prod.Product.Id);
                if (prod.Bom != null)
                {
                    allKnownParts.Add(prod.Bom.Id);
                }
            }
        }

        // Validate subassembly children - they must reference existing components or other subassemblies
        foreach (var (saId, sa) in subassemblies)
        {
            if (sa.Children == null) continue;

            foreach (var child in sa.Children)
            {
                if (!allKnownParts.Contains(child.Id))
                {
                    var childType = child.Type?.ToLowerInvariant() ?? "unknown";
                    errors.Add($"Subassembly '{saId}' references missing {childType}: '{child.Id}'");
                }
            }
        }

        // Validate product BOMs
        foreach (var (filePath, prod) in products)
        {
            if (prod.Bom?.Children == null) continue;

            ValidateBomNodeReferences(prod.Bom, allKnownParts, Path.GetFileName(filePath), errors);
        }

        // If there are validation errors, stop import
        if (errors.Count > 0)
        {
            return (false, $"Import validation failed:\n{string.Join("\n", errors.Take(20))}", 0);
        }

        // ========== PHASE 3: Import in correct order ==========

        // Create component parts first (they have no dependencies)
        foreach (var (id, comp) in components)
        {
            var existing = await _partTypeRepository.GetByPartNumberAsync(id);
            if (existing != null)
            {
                partIdMap[id] = existing.Id;
                warnings.Add($"Component '{id}' already exists");
            }
            else
            {
                var newPart = new PartType
                {
                    PartNumber = id,
                    Name = comp.Name,
                    Description = comp.Description,
                    Category = PartCategory.Component,
                    UnitOfMeasure = comp.UnitOfMeasure,
                    UnitCost = comp.UnitCost
                };
                var newId = await _partTypeRepository.CreateAsync(newPart);
                partIdMap[id] = newId;
                totalImported++;
            }
        }

        // Sort subassemblies by dependency level (leaf subassemblies first)
        var sortedSubassemblies = TopologicalSortSubassemblies(subassemblies);

        // Create subassembly parts in dependency order
        foreach (var saId in sortedSubassemblies)
        {
            var sa = subassemblies[saId];
            var existing = await _partTypeRepository.GetByPartNumberAsync(saId);
            if (existing != null)
            {
                partIdMap[saId] = existing.Id;
                warnings.Add($"Subassembly '{saId}' already exists");
            }
            else
            {
                var newPart = new PartType
                {
                    PartNumber = saId,
                    Name = sa.Name,
                    Category = PartCategory.SubAssembly,
                    UnitOfMeasure = "EA"
                };
                var newId = await _partTypeRepository.CreateAsync(newPart);
                partIdMap[saId] = newId;
                totalImported++;
            }
        }

        // Create BOMs for subassemblies (all parts now exist)
        foreach (var saId in sortedSubassemblies)
        {
            var sa = subassemblies[saId];
            if (sa.Children == null || sa.Children.Count == 0) continue;

            var node = new NestedBomNode
            {
                Id = saId,
                Name = sa.Name,
                Children = sa.Children
            };
            await CreateBomFromNestedNode(node, partIdMap);
        }

        // Import products (all subassemblies now exist)
        foreach (var (filePath, prod) in products)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var (success, error, count) = await ImportNestedBomAsync(json);
                if (success)
                {
                    totalImported += count;
                }
                else if (error != null)
                {
                    warnings.Add($"{Path.GetFileName(filePath)}: {error}");
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Error importing {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        // 4. Load product variants - from Products_variants folder
        if (Directory.Exists(variantsFolder))
        {
            var variantFiles = Directory.GetFiles(variantsFolder, "*.json");
            foreach (var file in variantFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var (success, error, count) = await ImportProductVariantAsync(json);
                    if (success)
                    {
                        totalImported += count;
                    }
                    else if (error != null)
                    {
                        warnings.Add($"{Path.GetFileName(file)}: {error}");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Error reading {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        if (warnings.Count > 0)
        {
            return (true, $"Imported with {warnings.Count} warning(s):\n{string.Join("\n", warnings.Take(10))}", totalImported);
        }

        return (true, null, totalImported);
    }

    /// <summary>
    /// Validates that all children in a BOM node exist in the known parts set
    /// </summary>
    private void ValidateBomNodeReferences(NestedBomNode node, HashSet<string> knownParts, string context, List<string> errors)
    {
        if (node.Children == null) return;

        foreach (var child in node.Children)
        {
            if (!knownParts.Contains(child.Id))
            {
                var childType = child.Type?.ToLowerInvariant() ?? "unknown";
                errors.Add($"{context}: BOM '{node.Id}' references missing {childType}: '{child.Id}'");
            }

            // Recursively check nested children
            if (child.Children != null && child.Children.Count > 0)
            {
                var childNode = new NestedBomNode
                {
                    Id = child.Id,
                    Name = child.Name,
                    Children = child.Children
                };
                ValidateBomNodeReferences(childNode, knownParts, context, errors);
            }
        }
    }

    /// <summary>
    /// Topologically sorts subassemblies so that dependencies are imported before dependents
    /// </summary>
    private List<string> TopologicalSortSubassemblies(Dictionary<string, SubassemblyDefinition> subassemblies)
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>(); // For cycle detection

        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (visiting.Contains(id))
            {
                // Cycle detected - skip to avoid infinite loop
                return;
            }

            visiting.Add(id);

            if (subassemblies.TryGetValue(id, out var sa) && sa.Children != null)
            {
                foreach (var child in sa.Children)
                {
                    if (child.Type?.ToLowerInvariant() == "subassembly" && subassemblies.ContainsKey(child.Id))
                    {
                        Visit(child.Id);
                    }
                }
            }

            visiting.Remove(id);
            visited.Add(id);
            result.Add(id);
        }

        foreach (var id in subassemblies.Keys)
        {
            Visit(id);
        }

        return result;
    }

    public async Task<(bool IsValid, List<string> Errors, List<string> Warnings)> ValidateImportAsync(string json)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var format = DetectFormat(json);

        if (format == ImportFormat.NestedBom)
        {
            return await ValidateNestedBomAsync(json);
        }

        if (format == ImportFormat.ComponentsList)
        {
            return await ValidateComponentsListAsync(json);
        }

        if (format == ImportFormat.SubassembliesList)
        {
            return await ValidateSubassembliesListAsync(json);
        }

        try
        {
            var import = JsonSerializer.Deserialize<PartImportExport>(json, JsonOptions);
            if (import == null)
            {
                errors.Add("Invalid JSON format");
                return (false, errors, warnings);
            }

            if (import.Parts.Count == 0)
            {
                errors.Add("No parts found in import data");
                return (false, errors, warnings);
            }

            var partNumbers = new HashSet<string>();
            var allPartNumbers = new HashSet<string>(import.Parts.Select(p => p.PartNumber));

            foreach (var item in import.Parts)
            {
                // Check for duplicate part numbers in import
                if (!partNumbers.Add(item.PartNumber))
                {
                    errors.Add($"Duplicate part number: {item.PartNumber}");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(item.PartNumber))
                {
                    errors.Add("Part number is required");
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    errors.Add($"Part name is required for {item.PartNumber}");
                }

                // Validate category
                if (!Enum.TryParse<PartCategory>(item.Category, true, out _))
                {
                    warnings.Add($"Unknown category '{item.Category}' for {item.PartNumber}, will use 'Component'");
                }

                // Check if part already exists
                var existingPart = await _partTypeRepository.GetByPartNumberAsync(item.PartNumber);
                if (existingPart != null)
                {
                    warnings.Add($"Part {item.PartNumber} already exists and will be updated");
                }

                // Validate BOM references
                if (item.Bom != null)
                {
                    foreach (var bomItem in item.Bom)
                    {
                        if (!allPartNumbers.Contains(bomItem.ComponentPartNumber))
                        {
                            // Check if component exists in database
                            var componentPart = await _partTypeRepository.GetByPartNumberAsync(bomItem.ComponentPartNumber);
                            if (componentPart == null)
                            {
                                errors.Add($"BOM for {item.PartNumber} references unknown component: {bomItem.ComponentPartNumber}");
                            }
                        }

                        // Check for self-reference
                        if (bomItem.ComponentPartNumber == item.PartNumber)
                        {
                            errors.Add($"Part {item.PartNumber} cannot contain itself in its BOM");
                        }
                    }
                }
            }

            return (errors.Count == 0, errors, warnings);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parsing error: {ex.Message}");
            return (false, errors, warnings);
        }
    }

    private async Task<(bool IsValid, List<string> Errors, List<string> Warnings)> ValidateNestedBomAsync(string json)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var import = JsonSerializer.Deserialize<NestedProductImport>(json, JsonOptions);
            if (import?.Product == null)
            {
                errors.Add("Invalid nested BOM format: missing 'product' object");
                return (false, errors, warnings);
            }

            if (import.Bom == null)
            {
                errors.Add("Invalid nested BOM format: missing 'bom' object");
                return (false, errors, warnings);
            }

            // Check if product already exists
            var existingProduct = await _partTypeRepository.GetByPartNumberAsync(import.Product.Id);
            if (existingProduct != null)
            {
                warnings.Add($"Product {import.Product.Id} already exists and will be updated");
            }

            // Collect all referenced parts and check for existence
            var allParts = new Dictionary<string, (string Name, PartCategory Category, double Qty)>();
            CollectPartsFromBom(import.Bom, allParts);

            foreach (var (partNumber, _) in allParts)
            {
                var existing = await _partTypeRepository.GetByPartNumberAsync(partNumber);
                if (existing != null)
                {
                    warnings.Add($"Part {partNumber} already exists");
                }
            }

            return (errors.Count == 0, errors, warnings);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parsing error: {ex.Message}");
            return (false, errors, warnings);
        }
    }

    private async Task<(bool IsValid, List<string> Errors, List<string> Warnings)> ValidateComponentsListAsync(string json)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var wrapper = JsonSerializer.Deserialize<ComponentsFileWrapper>(json, JsonOptions);
            if (wrapper?.Components == null || wrapper.Components.Count == 0)
            {
                errors.Add("No components found in file");
                return (false, errors, warnings);
            }

            var ids = new HashSet<string>();
            foreach (var comp in wrapper.Components)
            {
                if (string.IsNullOrWhiteSpace(comp.Id))
                {
                    errors.Add("Component ID is required");
                    continue;
                }

                if (!ids.Add(comp.Id))
                {
                    errors.Add($"Duplicate component ID: {comp.Id}");
                }

                if (string.IsNullOrWhiteSpace(comp.Name))
                {
                    warnings.Add($"Component {comp.Id} has no name");
                }

                var existing = await _partTypeRepository.GetByPartNumberAsync(comp.Id);
                if (existing != null)
                {
                    warnings.Add($"Component {comp.Id} already exists and will be updated");
                }
            }

            return (errors.Count == 0, errors, warnings);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parsing error: {ex.Message}");
            return (false, errors, warnings);
        }
    }

    private async Task<(bool IsValid, List<string> Errors, List<string> Warnings)> ValidateSubassembliesListAsync(string json)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var wrapper = JsonSerializer.Deserialize<SubassembliesFileWrapper>(json, JsonOptions);
            if (wrapper?.Subassemblies == null || wrapper.Subassemblies.Count == 0)
            {
                errors.Add("No subassemblies found in file");
                return (false, errors, warnings);
            }

            var ids = new HashSet<string>();
            var allChildIds = new HashSet<string>();

            foreach (var sa in wrapper.Subassemblies)
            {
                if (string.IsNullOrWhiteSpace(sa.Id))
                {
                    errors.Add("Subassembly ID is required");
                    continue;
                }

                if (!ids.Add(sa.Id))
                {
                    errors.Add($"Duplicate subassembly ID: {sa.Id}");
                }

                if (string.IsNullOrWhiteSpace(sa.Name))
                {
                    warnings.Add($"Subassembly {sa.Id} has no name");
                }

                var existing = await _partTypeRepository.GetByPartNumberAsync(sa.Id);
                if (existing != null)
                {
                    warnings.Add($"Subassembly {sa.Id} already exists and will be updated");
                }

                // Collect child references
                if (sa.Children != null)
                {
                    foreach (var child in sa.Children)
                    {
                        allChildIds.Add(child.Id);
                    }
                }
            }

            // Check if referenced children exist (either in this file or in database)
            foreach (var childId in allChildIds)
            {
                if (!ids.Contains(childId))
                {
                    var existing = await _partTypeRepository.GetByPartNumberAsync(childId);
                    if (existing == null)
                    {
                        warnings.Add($"Referenced part {childId} not found in file or database - import components first");
                    }
                }
            }

            return (errors.Count == 0, errors, warnings);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parsing error: {ex.Message}");
            return (false, errors, warnings);
        }
    }

    private static PartCategory ParseCategory(string category)
    {
        if (Enum.TryParse<PartCategory>(category, true, out var result))
        {
            return result;
        }
        return PartCategory.Component;
    }

    /// <summary>
    /// Parses subassemblies JSON, handling both wrapped {"subassemblies": [...]} and direct [...] array formats
    /// </summary>
    private static List<SubassemblyDefinition>? ParseSubassembliesJson(string json)
    {
        // Try wrapped format first: {"subassemblies": [...]}
        try
        {
            var wrapper = JsonSerializer.Deserialize<SubassembliesFileWrapper>(json, JsonOptions);
            if (wrapper?.Subassemblies != null && wrapper.Subassemblies.Count > 0)
            {
                return wrapper.Subassemblies;
            }
        }
        catch
        {
            // Ignore and try direct array format
        }

        // Try direct array format: [...]
        try
        {
            return JsonSerializer.Deserialize<List<SubassemblyDefinition>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses components JSON, handling both wrapped {"components": [...]} and direct [...] array formats
    /// </summary>
    private static List<ComponentDefinition>? ParseComponentsJson(string json)
    {
        try
        {
            // Try wrapped format first: {"components": [...]}
            var wrapper = JsonSerializer.Deserialize<ComponentsFileWrapper>(json, JsonOptions);
            if (wrapper?.Components != null)
            {
                return wrapper.Components;
            }
        }
        catch
        {
            // Ignore and try direct array format
        }

        try
        {
            // Try direct array format: [...]
            return JsonSerializer.Deserialize<List<ComponentDefinition>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ImportPreviewData?> BuildImportPreviewAsync(string json)
    {
        var format = DetectFormat(json);
        if (format != ImportFormat.NestedBom)
        {
            return null; // Only support preview for nested BOM format
        }

        try
        {
            var import = JsonSerializer.Deserialize<NestedProductImport>(json, JsonOptions);
            if (import?.Product == null || import.Bom == null)
            {
                return null;
            }

            var preview = new ImportPreviewData
            {
                ProductId = import.Product.Id,
                ProductName = import.Product.Name
            };

            // Collect all parts to check existence
            var allParts = new Dictionary<string, (string Name, PartCategory Category, double Qty)>();
            CollectPartsFromBom(import.Bom, allParts);
            allParts[import.Product.Id] = (import.Product.Name, PartCategory.FinishedGood, 1);

            // Check which parts exist
            var existingParts = new HashSet<string>();
            foreach (var partNumber in allParts.Keys)
            {
                var existing = await _partTypeRepository.GetByPartNumberAsync(partNumber);
                if (existing != null)
                {
                    existingParts.Add(partNumber);
                }
            }

            // Build preview tree
            var rootNode = new ImportPreviewNode
            {
                PartNumber = import.Product.Id,
                PartName = import.Product.Name,
                Category = "FinishedGood",
                IsNew = !existingParts.Contains(import.Product.Id),
                IsRoot = true
            };

            if (import.Bom.Children != null)
            {
                foreach (var child in import.Bom.Children)
                {
                    var childNode = BuildPreviewNode(child, existingParts, allParts);
                    rootNode.Children.Add(childNode);
                }
            }

            preview.RootNodes.Add(rootNode);
            preview.TotalParts = allParts.Count;
            preview.NewParts = allParts.Count - existingParts.Count;
            preview.ExistingParts = existingParts.Count;

            return preview;
        }
        catch
        {
            return null;
        }
    }

    private ImportPreviewNode BuildPreviewNode(
        NestedBomChild child,
        HashSet<string> existingParts,
        Dictionary<string, (string Name, PartCategory Category, double Qty)> allParts)
    {
        var category = child.Type?.ToLowerInvariant() switch
        {
            "component" => "Component",
            "subassembly" => "SubAssembly",
            "raw" or "rawmaterial" => "RawMaterial",
            _ => "Component"
        };

        var node = new ImportPreviewNode
        {
            PartNumber = child.Id,
            PartName = allParts.TryGetValue(child.Id, out var info) ? info.Name : child.Name ?? child.Id,
            Category = category,
            Quantity = (decimal)child.Qty,
            IsNew = !existingParts.Contains(child.Id),
            IsRoot = false
        };

        if (child.Children != null)
        {
            foreach (var grandchild in child.Children)
            {
                node.Children.Add(BuildPreviewNode(grandchild, existingParts, allParts));
            }
        }

        return node;
    }

    public async Task<(bool Success, string? Error, int ImportedCount)> ImportWithModificationsAsync(
        string json,
        Dictionary<string, decimal> modifiedQuantities)
    {
        var format = DetectFormat(json);
        if (format != ImportFormat.NestedBom)
        {
            return await ImportFromJsonAsync(json);
        }

        try
        {
            var import = JsonSerializer.Deserialize<NestedProductImport>(json, JsonOptions);
            if (import?.Product == null || import.Bom == null)
            {
                return (false, "Invalid nested BOM format", 0);
            }

            // Apply quantity modifications to the BOM
            if (import.Bom.Children != null)
            {
                ApplyQuantityModifications(import.Bom.Children, modifiedQuantities);
            }

            // Re-serialize and import
            var modifiedJson = JsonSerializer.Serialize(import, JsonOptions);
            return await ImportNestedBomAsync(modifiedJson);
        }
        catch (Exception ex)
        {
            return (false, $"Import error: {ex.Message}", 0);
        }
    }

    private void ApplyQuantityModifications(List<NestedBomChild> children, Dictionary<string, decimal> modifications)
    {
        foreach (var child in children)
        {
            if (modifications.TryGetValue(child.Id, out var newQty))
            {
                child.Qty = (double)newQty;
            }

            if (child.Children != null)
            {
                ApplyQuantityModifications(child.Children, modifications);
            }
        }
    }
}
