@echo off
echo =============================================
echo Auto-ID Generation Fix Script
echo =============================================
echo.

powershell -NoProfile -Command "(Get-Content NodeModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content NodeModels.cs"
echo Updated: NodeModels.cs

powershell -NoProfile -Command "(Get-Content PathModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content PathModels.cs"
echo Updated: PathModels.cs

powershell -NoProfile -Command "(Get-Content WallModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content WallModels.cs"
echo Updated: WallModels.cs

powershell -NoProfile -Command "(Get-Content GroupModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content GroupModels.cs"
echo Updated: GroupModels.cs

powershell -NoProfile -Command "(Get-Content LayerModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content LayerModels.cs"
echo Updated: LayerModels.cs

powershell -NoProfile -Command "(Get-Content LayoutData.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content LayoutData.cs"
echo Updated: LayoutData.cs

powershell -NoProfile -Command "(Get-Content ZoneModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content ZoneModels.cs"
echo Updated: ZoneModels.cs

powershell -NoProfile -Command "(Get-Content SimulationModels.cs) -replace 'public string Id { get; set; }', 'public string Id { get; set; } = Guid.NewGuid().ToString();' | Set-Content SimulationModels.cs"
echo Updated: SimulationModels.cs

echo.
echo =============================================
echo Done! Run 'dotnet build' to verify.
echo =============================================
pause
