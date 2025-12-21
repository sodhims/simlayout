# Week 2: Layer 0 (Infrastructure) + Layer 1 (Spatial) - Progress Report

## Status: IN PROGRESS (30% Complete)

---

## ✅ Completed Tasks

### Task 2.1: Create Door Model ✅
**File:** `Models/WallModels.cs` (enhanced)

Enhanced existing `DoorData` model with:
- ✅ Added `Name` property
- ✅ Added `X`, `Y` position (for standalone doors)
- ✅ Added `Height` property (default: 80)
- ✅ Added `Rotation` property (degrees)
- ✅ Enhanced `DoorType` with Personnel, Dock, Fire, Emergency types
- ✅ Added `SwingAngle` property (swing arc in degrees, default: 90)
- ✅ Added `SwingDirection` property (inward/outward/sliding/bidirectional)
- ✅ Added `TransportLayer` property returning `LayerType.Infrastructure`
- ✅ Created `DoorTypes` constants
- ✅ Created `SwingDirections` constants

### Task 2.2: Create Crane Runway Model ✅
**File:** `Models/CraneModels.cs` (enhanced)

Enhanced existing `RunwayData` model with:
- ✅ Existing properties: Id, Name, StartX, StartY, EndX, EndY, Height, Color
- ✅ Added `Capacity` property (kg, default: 5000)
- ✅ Added `RunwayPairId` property (to link parallel runways)
- ✅ Added `TransportLayer` property returning `LayerType.Infrastructure`
- ✅ Existing helper methods: `GetPositionAt()`, `GetParameterAt()`, `GetPerpendicular()`

### Infrastructure Foundation ✅
**File:** `Renderers/ILayerRenderer.cs`

Created `ILayerRenderer` interface with:
- ✅ `Layer` property (LayerType)
- ✅ `ZOrderBase` property (int)
- ✅ `Render()` method (Canvas, LayoutData, registerElement callback)

---

## 📋 Remaining Tasks

### Task 2.3: Add Infrastructure Collections to LayoutData
**File to Modify:** `Models/LayoutData.cs`

**Note:** `Doors` collection already exists in LayoutData (line 55)
**Note:** `Runways` collection already exists in LayoutData (line 71)

**Action Required:** Verify these collections are properly serialized

### Task 2.4: Create Infrastructure Renderer
**New File:** `Renderers/InfrastructureRenderer.cs`

```csharp
public class InfrastructureRenderer : ILayerRenderer
{
    public LayerType Layer => LayerType.Infrastructure;
    public int ZOrderBase => 0; // 0-99 range

    public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
    {
        // Extract wall rendering from WallRenderer
        // Extract column rendering from WallRenderer
        // Add door rendering
        // Add runway rendering
    }
}
```

### Task 2.5: Create Primary Aisle Model
**New File:** `Models/AisleModels.cs`

```csharp
public class PrimaryAisleData : NotifyBase
{
    // Properties: Id, Name, Centerline (ObservableCollection<PointData>), Width
    // IsEmergencyRoute flag
    // AisleType: Main, Secondary, Emergency
    // TransportLayer property returns Spatial
}
```

### Task 2.6: Create Restricted Area Model
**New File or add to:** `Models/ZoneModels.cs`

```csharp
public class RestrictedAreaData : NotifyBase
{
    // Properties: Id, Name, X, Y, Width, Height
    // RestrictionType: AuthorizedOnly, Hazmat, Cleanroom, HighVoltage
    // RequiredPPE field (text)
    // TransportLayer property returns Spatial
}
```

### Task 2.7: Add Spatial Collections to LayoutData
**File to Modify:** `Models/LayoutData.cs`

- Add `PrimaryAisles` collection (ObservableCollection<PrimaryAisleData>)
- Add `RestrictedAreas` collection (ObservableCollection<RestrictedAreaData>)
- **Note:** `Zones` collection already exists

### Task 2.8: Create Spatial Renderer
**New File:** `Renderers/SpatialRenderer.cs`

```csharp
public class SpatialRenderer : ILayerRenderer
{
    public LayerType Layer => LayerType.Spatial;
    public int ZOrderBase => 100; // 100-199 range

    public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
    {
        // Extract zone rendering from GroupRenderer
        // Add primary aisle rendering (corridors with dashed edges)
        // Emergency routes render in yellow
        // Add restricted area rendering (warning patterns)
    }
}
```

### Task 2.9: Modify Redraw to Use Layer Renderers
**File to Modify:** `MainWindow.xaml.cs`

Current `Redraw()` method needs refactoring:
1. Initialize layer renderers dictionary
2. Clear canvas
3. Draw grid
4. Get visible layers from `_transportLayerManager.GetVisibleLayers()`
5. Iterate visible layers in Z-order
6. Call appropriate renderer for each layer
7. Draw selection handles last

### Task 2.10: Modify Selection to Respect Layers
**Files to Modify:** `Services/HitTestService.cs`, `Handlers/*Handlers.cs`

Requirements:
- Hit test only visible layers
- Hit test only editable layers
- Test layers in reverse Z-order (top first)
- Return first hit found

---

## Testing Requirements

Once all tasks are complete, create tests for:

| Test ID | Description |
|---------|-------------|
| T2.1 | DoorData has Infrastructure layer |
| T2.2 | RunwayData has Infrastructure layer |
| T2.3 | PrimaryAisleData has Spatial layer |
| T2.4 | RestrictedAreaData has Spatial layer |
| T2.5 | Hide Infrastructure hides walls |
| T2.6 | Hide Spatial hides zones |
| T2.7 | Hidden layer elements not selectable |
| T2.8 | Locked layer elements not editable |
| T2.9 | Save includes new collections |
| T2.10 | Infrastructure renders below Spatial |

---

## Files Created So Far

1. `Renderers/ILayerRenderer.cs` - Layer renderer interface

## Files Modified So Far

1. `Models/WallModels.cs` - Enhanced DoorData model
2. `Models/CraneModels.cs` - Enhanced RunwayData model

---

## Next Steps

1. Verify Doors and Runways collections exist in LayoutData
2. Create AisleModels.cs with PrimaryAisleData
3. Add RestrictedAreaData to ZoneModels.cs
4. Update LayoutData with new collections
5. Create InfrastructureRenderer
6. Create SpatialRenderer
7. Refactor Redraw() method
8. Update HitTestService for layer awareness
9. Create Week 2 test suite
10. Build and test

---

**Date:** 2025-12-20
**Progress:** 3/10 tasks complete (30%)
