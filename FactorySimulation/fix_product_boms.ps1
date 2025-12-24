$dbPath = "r:\layoutbak\FactorySimulation\FactorySimulation.Configurator\bin\Debug\net9.0-windows\factory.db"
$productsFolder = "r:\layoutbak\FactoryData\Products"

# Get all product JSON files
$productFiles = Get-ChildItem -Path $productsFolder -Filter "PROD-*.json"

foreach ($file in $productFiles) {
    $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
    $productId = $json.product.id
    $bomId = $json.bom.id

    Write-Host "Processing $productId -> $bomId"

    # Get the product's database ID
    $productDbId = sqlite3 $dbPath "SELECT Id FROM PartTypes WHERE PartNumber = '$productId'"
    if (-not $productDbId) {
        Write-Host "  Product $productId not found in database"
        continue
    }

    # Get the root subassembly's database ID
    $saDbId = sqlite3 $dbPath "SELECT Id FROM PartTypes WHERE PartNumber = '$bomId'"
    if (-not $saDbId) {
        Write-Host "  Subassembly $bomId not found in database"
        continue
    }

    # Check if product already has a BOM
    $existingBom = sqlite3 $dbPath "SELECT Id FROM BillOfMaterials WHERE PartTypeId = $productDbId"
    if ($existingBom) {
        Write-Host "  Product $productId already has BOM $existingBom, clearing items..."
        sqlite3 $dbPath "DELETE FROM BOMItems WHERE BomId = $existingBom"
    } else {
        Write-Host "  Creating new BOM for $productId..."
        sqlite3 $dbPath "INSERT INTO BillOfMaterials (PartTypeId, Version, IsActive) VALUES ($productDbId, 1, 1)"
        $existingBom = sqlite3 $dbPath "SELECT Id FROM BillOfMaterials WHERE PartTypeId = $productDbId"
    }

    # Get the SA's BOM ID
    $saBomId = sqlite3 $dbPath "SELECT Id FROM BillOfMaterials WHERE PartTypeId = $saDbId"
    if (-not $saBomId) {
        Write-Host "  Subassembly $bomId has no BOM"
        continue
    }

    # Copy BOM items from SA to product
    $count = sqlite3 $dbPath "SELECT COUNT(*) FROM BOMItems WHERE BomId = $saBomId"
    Write-Host "  Copying $count items from SA BOM $saBomId to Product BOM $existingBom..."
    sqlite3 $dbPath "INSERT INTO BOMItems (BomId, ComponentPartTypeId, Quantity, UnitOfMeasure, Sequence) SELECT $existingBom, ComponentPartTypeId, Quantity, UnitOfMeasure, Sequence FROM BOMItems WHERE BomId = $saBomId"

    $newCount = sqlite3 $dbPath "SELECT COUNT(*) FROM BOMItems WHERE BomId = $existingBom"
    Write-Host "  Done. Product $productId now has $newCount BOM items."
}

Write-Host "`nAll products processed."
