using System.Windows;
using System.Linq;
using System.Windows.Controls;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Context Menu

        private Point _contextMenuPosition;

        private void ShowContextMenu(Point position)
        {
            var hitResult = _hitTestService.HitTest(_layout, position, _transportArchitectureLayerManager,
                _layout.FrictionlessMode, _layout.DesignMode);

            CanvasContextMenu.Items.Clear();

            if (hitResult.Type == HitType.Node && hitResult.Node != null)
            {
                BuildNodeContextMenu(hitResult.Node);
            }
            else if (hitResult.Type == HitType.Path && hitResult.Path != null)
            {
                BuildPathContextMenu(hitResult.Path);
            }
            else if (hitResult.Type == HitType.GroupBorder && hitResult.Group != null)
            {
                BuildGroupContextMenu(hitResult.Group);
            }
            else if (hitResult.Type == HitType.EOTCrane && hitResult.EOTCrane != null)
            {
                BuildEOTCraneContextMenu(hitResult.EOTCrane);
            }
            else if (hitResult.Type == HitType.JibCrane && hitResult.JibCrane != null)
            {
                BuildJibCraneContextMenu(hitResult.JibCrane);
            }
            else if (hitResult.Type == HitType.Zone && hitResult.Zone != null)
            {
                BuildZoneContextMenu(hitResult.Zone);
            }
            else if (hitResult.Type == HitType.AGVStation && hitResult.AGVStation != null)
            {
                BuildAGVStationContextMenu(hitResult.AGVStation);
            }
            else if (hitResult.Type == HitType.Runway && hitResult.Runway != null)
            {
                BuildRunwayContextMenu(hitResult.Runway);
            }
            else if (hitResult.Type == HitType.Opening && hitResult.Opening != null)
            {
                BuildOpeningContextMenu(hitResult.Opening);
            }
            else
            {
                BuildCanvasContextMenu();
            }
        }

        private void BuildNodeContextMenu(NodeData node)
        {
            // Ensure node is selected
            if (!_selectionService.IsNodeSelected(node.Id))
            {
                _selectionService.SelectNode(node.Id);
                UpdateSelectionVisuals();
            }

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties", (s, e) => {
                _panelManager?.ShowNodeProperties(node);
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Change icon submenu
            var iconMenu = new MenuItem { Header = "Change Icon" };
            var suggestedIcons = IconLibrary.GetSuggestedIcons(node.Type ?? NodeTypes.Machine);

            foreach (var iconKey in suggestedIcons)
            {
                if (IconLibrary.Icons.TryGetValue(iconKey, out var iconDef))
                {
                    var item = CreateMenuItem(iconDef.Name, (s, e) => {
                        SaveUndoState();
                        node.Visual.Icon = iconKey;
                        MarkDirty();
                        Redraw();
                    });
                    item.IsChecked = node.Visual.Icon == iconKey;
                    iconMenu.Items.Add(item);
                }
            }
            CanvasContextMenu.Items.Add(iconMenu);

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Start Path From Here", (s, e) => {
                _pathStartNodeId = node.Id;
                _isDrawingPath = true;
                StatusText.Text = "Click destination node";
            }));

            CanvasContextMenu.Items.Add(CreateMenuItem("Duplicate", (s, e) => Duplicate_Click(s, e)));
            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => Delete_Click(s, e)));

            if (_selectionService.SelectedCount >= 2)
            {
                CanvasContextMenu.Items.Add(new Separator());
                CanvasContextMenu.Items.Add(CreateMenuItem($"Create Cell ({_selectionService.SelectedCount} nodes)",
                    (s, e) => DefineCell_Click(s, e)));
            }
        }

        private void BuildPathContextMenu(PathData path)
        {
            _selectionService.SelectPath(path.Id);
            UpdateSelectionVisuals();

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties", (s, e) => {
                _panelManager?.ShowPathProperties(path);
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Routing mode submenu
            var routingMenu = new MenuItem { Header = "Routing Mode" };
            routingMenu.Items.Add(CreateCheckedMenuItem("Direct", path.RoutingMode == RoutingModes.Direct,
                (s, e) => { path.RoutingMode = RoutingModes.Direct; MarkDirty(); Redraw(); }));
            routingMenu.Items.Add(CreateCheckedMenuItem("Manhattan", path.RoutingMode == RoutingModes.Manhattan,
                (s, e) => { path.RoutingMode = RoutingModes.Manhattan; MarkDirty(); Redraw(); }));
            routingMenu.Items.Add(CreateCheckedMenuItem("Curved", path.RoutingMode == RoutingModes.Corridor,
                (s, e) => { path.RoutingMode = RoutingModes.Corridor; MarkDirty(); Redraw(); }));
            CanvasContextMenu.Items.Add(routingMenu);

            // Waypoint options
            CanvasContextMenu.Items.Add(new Separator());
            CanvasContextMenu.Items.Add(CreateMenuItem("Add Waypoint at Click", (s, e) => {
                SaveUndoState();
                path.Visual.Waypoints.Add(new PointData(_contextMenuPosition.X, _contextMenuPosition.Y));
                MarkDirty();
                Redraw();
                StatusText.Text = "Waypoint added";
            }));
            
            if (path.Visual.Waypoints.Count > 0)
            {
                CanvasContextMenu.Items.Add(CreateMenuItem($"Clear All Waypoints ({path.Visual.Waypoints.Count})", (s, e) => {
                    SaveUndoState();
                    path.Visual.Waypoints.Clear();
                    MarkDirty();
                    Redraw();
                    StatusText.Text = "Waypoints cleared";
                }));
            }

            CanvasContextMenu.Items.Add(new Separator());
            CanvasContextMenu.Items.Add(CreateMenuItem("Delete Path", (s, e) => DeletePath(path.Id)));
        }

        private void BuildGroupContextMenu(GroupData group)
        {
            // For cells, include internal paths in selection
            if (group.IsCell)
            {
                SelectCellWithPaths(group);
            }
            else
            {
                _selectionService.SelectGroup(group.Id, group.Members);
            }
            UpdateSelectionVisuals();

            var headerItem = CreateMenuItem($"Group: {group.Name}", null);
            headerItem.IsEnabled = false;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            // Properties option
            CanvasContextMenu.Items.Add(CreateMenuItem("Properties", (s, e) => {
                _panelManager?.ShowGroupProperties(group);
            }));

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateCheckedMenuItem("Is Cell", group.IsCell,
                (s, e) => { group.IsCell = !group.IsCell; MarkDirty(); Redraw(); }));

            if (group.IsCell)
            {
                CanvasContextMenu.Items.Add(new Separator());
                
                // Input terminal position
                var inputMenu = new MenuItem { Header = "Input Terminal" };
                inputMenu.Items.Add(CreateCheckedMenuItem("Left", group.InputTerminalPosition == "left",
                    (s, e) => { group.InputTerminalPosition = "left"; MarkDirty(); Redraw(); }));
                inputMenu.Items.Add(CreateCheckedMenuItem("Right", group.InputTerminalPosition == "right",
                    (s, e) => { group.InputTerminalPosition = "right"; MarkDirty(); Redraw(); }));
                inputMenu.Items.Add(CreateCheckedMenuItem("Top", group.InputTerminalPosition == "top",
                    (s, e) => { group.InputTerminalPosition = "top"; MarkDirty(); Redraw(); }));
                inputMenu.Items.Add(CreateCheckedMenuItem("Bottom", group.InputTerminalPosition == "bottom",
                    (s, e) => { group.InputTerminalPosition = "bottom"; MarkDirty(); Redraw(); }));
                CanvasContextMenu.Items.Add(inputMenu);

                // Output terminal position
                var outputMenu = new MenuItem { Header = "Output Terminal" };
                outputMenu.Items.Add(CreateCheckedMenuItem("Left", group.OutputTerminalPosition == "left",
                    (s, e) => { group.OutputTerminalPosition = "left"; MarkDirty(); Redraw(); }));
                outputMenu.Items.Add(CreateCheckedMenuItem("Right", group.OutputTerminalPosition == "right",
                    (s, e) => { group.OutputTerminalPosition = "right"; MarkDirty(); Redraw(); }));
                outputMenu.Items.Add(CreateCheckedMenuItem("Top", group.OutputTerminalPosition == "top",
                    (s, e) => { group.OutputTerminalPosition = "top"; MarkDirty(); Redraw(); }));
                outputMenu.Items.Add(CreateCheckedMenuItem("Bottom", group.OutputTerminalPosition == "bottom",
                    (s, e) => { group.OutputTerminalPosition = "bottom"; MarkDirty(); Redraw(); }));
                CanvasContextMenu.Items.Add(outputMenu);

                // Swap terminals
                CanvasContextMenu.Items.Add(CreateMenuItem("Swap Input/Output", (s, e) => {
                    var temp = group.InputTerminalPosition;
                    group.InputTerminalPosition = group.OutputTerminalPosition;
                    group.OutputTerminalPosition = temp;
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Terminals swapped: In={group.InputTerminalPosition}, Out={group.OutputTerminalPosition}";
                }));

                // Quick presets
                var presetsMenu = new MenuItem { Header = "Terminal Presets" };
                presetsMenu.Items.Add(CreateMenuItem("← In | Out →  (Left to Right)", (s, e) => {
                    group.InputTerminalPosition = "left";
                    group.OutputTerminalPosition = "right";
                    MarkDirty(); Redraw();
                }));
                presetsMenu.Items.Add(CreateMenuItem("→ In | Out ←  (Right to Left)", (s, e) => {
                    group.InputTerminalPosition = "right";
                    group.OutputTerminalPosition = "left";
                    MarkDirty(); Redraw();
                }));
                presetsMenu.Items.Add(CreateMenuItem("↓ In | Out ↑  (Top to Bottom)", (s, e) => {
                    group.InputTerminalPosition = "top";
                    group.OutputTerminalPosition = "bottom";
                    MarkDirty(); Redraw();
                }));
                presetsMenu.Items.Add(CreateMenuItem("↑ In | Out ↓  (Bottom to Top)", (s, e) => {
                    group.InputTerminalPosition = "bottom";
                    group.OutputTerminalPosition = "top";
                    MarkDirty(); Redraw();
                }));
                CanvasContextMenu.Items.Add(presetsMenu);
            }

            CanvasContextMenu.Items.Add(new Separator());
            CanvasContextMenu.Items.Add(CreateMenuItem("Ungroup", (s, e) => {
                SaveUndoState();
                _layout.Groups.Remove(group);
                MarkDirty();
                RefreshAll();
            }));
        }

        private void BuildCanvasContextMenu()
        {
            var addNodeMenu = new MenuItem { Header = "Add Node" };
            addNodeMenu.Items.Add(CreateMenuItem("Source", (s, e) => PlaceNodeAtPosition("source", _contextMenuPosition)));
            addNodeMenu.Items.Add(CreateMenuItem("Sink", (s, e) => PlaceNodeAtPosition("sink", _contextMenuPosition)));
            addNodeMenu.Items.Add(CreateMenuItem("Machine", (s, e) => PlaceNodeAtPosition("machine", _contextMenuPosition)));
            addNodeMenu.Items.Add(CreateMenuItem("Buffer", (s, e) => PlaceNodeAtPosition("buffer", _contextMenuPosition)));
            addNodeMenu.Items.Add(CreateMenuItem("Workstation", (s, e) => PlaceNodeAtPosition("workstation", _contextMenuPosition)));
            CanvasContextMenu.Items.Add(addNodeMenu);

            CanvasContextMenu.Items.Add(new Separator());
            CanvasContextMenu.Items.Add(CreateMenuItem("Paste Here", (s, e) => Paste_Click(s, e)));
        }

        private MenuItem CreateMenuItem(string header, RoutedEventHandler? handler)
        {
            var item = new MenuItem { Header = header };
            if (handler != null)
                item.Click += handler;
            return item;
        }

        private MenuItem CreateCheckedMenuItem(string header, bool isChecked, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = header, IsChecked = isChecked };
            item.Click += handler;
            return item;
        }

        private void BuildEOTCraneContextMenu(EOTCraneData crane)
        {
            var headerItem = CreateMenuItem($"EOT Crane: {crane.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties...", (s, e) => {
                var dialog = new Dialogs.EOTCranePropertiesDialog(crane, _layout);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Updated EOT crane '{crane.Name}'";
                }
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Zone position quick adjust
            var zoneMenu = new MenuItem { Header = "Adjust Zone" };
            zoneMenu.Items.Add(CreateMenuItem("Move to Start", (s, e) => {
                SaveUndoState();
                crane.BridgePosition = crane.ZoneMin;
                MarkDirty();
                Redraw();
            }));
            zoneMenu.Items.Add(CreateMenuItem("Move to Center", (s, e) => {
                SaveUndoState();
                crane.BridgePosition = (crane.ZoneMin + crane.ZoneMax) / 2;
                MarkDirty();
                Redraw();
            }));
            zoneMenu.Items.Add(CreateMenuItem("Move to End", (s, e) => {
                SaveUndoState();
                crane.BridgePosition = crane.ZoneMax;
                MarkDirty();
                Redraw();
            }));
            CanvasContextMenu.Items.Add(zoneMenu);

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                SaveUndoState();
                _layout.EOTCranes.Remove(crane);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted EOT crane '{crane.Name}'";
            }));
        }

        private void BuildJibCraneContextMenu(JibCraneData jib)
        {
            var headerItem = CreateMenuItem($"Jib Crane: {jib.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties...", (s, e) => {
                // TODO: Create JibCranePropertiesDialog
                MessageBox.Show($"Jib Crane: {jib.Name}\n" +
                    $"Center: ({jib.CenterX:F1}, {jib.CenterY:F1})\n" +
                    $"Radius: {jib.Radius:F1}\n" +
                    $"Arc: {jib.ArcStart}° to {jib.ArcEnd}°\n" +
                    $"Slew Speed: {jib.SpeedSlew:F1}°/s",
                    "Jib Crane Properties", MessageBoxButton.OK, MessageBoxImage.Information);
            }));

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                SaveUndoState();
                _layout.JibCranes.Remove(jib);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted jib crane '{jib.Name}'";
            }));
        }

        private void BuildZoneContextMenu(ZoneData zone)
        {
            var headerItem = CreateMenuItem($"Zone: {zone.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties...", (s, e) => {
                var dialog = new Dialogs.ZonePropertiesDialog(zone);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Updated zone '{zone.Name}'";
                }
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Zone type submenu
            var typeMenu = new MenuItem { Header = "Zone Type" };
            typeMenu.Items.Add(CreateCheckedMenuItem("Storage", zone.Type == "storage",
                (s, e) => { zone.Type = "storage"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Work Area", zone.Type == "work",
                (s, e) => { zone.Type = "work"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Staging", zone.Type == "staging",
                (s, e) => { zone.Type = "staging"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Restricted", zone.Type == "restricted",
                (s, e) => { zone.Type = "restricted"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Shipping", zone.Type == "shipping",
                (s, e) => { zone.Type = "shipping"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Receiving", zone.Type == "receiving",
                (s, e) => { zone.Type = "receiving"; MarkDirty(); Redraw(); }));
            CanvasContextMenu.Items.Add(typeMenu);

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                SaveUndoState();
                _layout.Zones.Remove(zone);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted zone '{zone.Name}'";
            }));
        }

        private void BuildAGVStationContextMenu(AGVStationData station)
        {
            var headerItem = CreateMenuItem($"AGV Station: {station.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties...", (s, e) => {
                var dialog = new Dialogs.AGVStationPropertiesDialog(station);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Updated AGV station '{station.Name}'";
                }
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Station type submenu
            var typeMenu = new MenuItem { Header = "Station Type" };
            typeMenu.Items.Add(CreateCheckedMenuItem("Pickup", station.StationType == "pickup",
                (s, e) => { station.StationType = "pickup"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Dropoff", station.StationType == "dropoff",
                (s, e) => { station.StationType = "dropoff"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Dual (Pickup/Dropoff)", station.StationType == "dual",
                (s, e) => { station.StationType = "dual"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Charging", station.StationType == "charging",
                (s, e) => { station.StationType = "charging"; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Parking", station.StationType == "parking",
                (s, e) => { station.StationType = "parking"; MarkDirty(); Redraw(); }));
            CanvasContextMenu.Items.Add(typeMenu);

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateCheckedMenuItem("Home Station", station.IsHoming,
                (s, e) => { station.IsHoming = !station.IsHoming; MarkDirty(); Redraw(); }));

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                SaveUndoState();
                _layout.AGVStations.Remove(station);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted AGV station '{station.Name}'";
            }));
        }

        private void BuildRunwayContextMenu(RunwayData runway)
        {
            var headerItem = CreateMenuItem($"Runway: {runway.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            // Show runway info
            double length = System.Math.Sqrt(
                System.Math.Pow(runway.EndX - runway.StartX, 2) +
                System.Math.Pow(runway.EndY - runway.StartY, 2));

            CanvasContextMenu.Items.Add(CreateMenuItem($"Length: {length:F1} px", null));

            // Count cranes on this runway
            int craneCount = _layout.EOTCranes.Count(c => c.RunwayId == runway.Id);
            CanvasContextMenu.Items.Add(CreateMenuItem($"Cranes: {craneCount}", null));

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                // Check if any cranes use this runway
                var cranesOnRunway = _layout.EOTCranes.Where(c => c.RunwayId == runway.Id).ToList();
                if (cranesOnRunway.Any())
                {
                    var result = MessageBox.Show(
                        $"This runway has {cranesOnRunway.Count} crane(s). Delete them too?",
                        "Delete Runway", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Cancel) return;
                    if (result == MessageBoxResult.Yes)
                    {
                        SaveUndoState();
                        foreach (var crane in cranesOnRunway)
                            _layout.EOTCranes.Remove(crane);
                    }
                }
                else
                {
                    SaveUndoState();
                }

                _layout.Runways.Remove(runway);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted runway '{runway.Name}'";
            }));
        }

        private void BuildOpeningContextMenu(OpeningData opening)
        {
            var headerItem = CreateMenuItem($"Opening: {opening.Name}", null);
            headerItem.IsEnabled = false;
            headerItem.FontWeight = FontWeights.SemiBold;
            CanvasContextMenu.Items.Add(headerItem);
            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Properties...", (s, e) => {
                var dialog = new Dialogs.OpeningPropertiesDialog(opening);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Updated opening '{opening.Name}'";
                }
            }));

            CanvasContextMenu.Items.Add(new Separator());

            // Opening type submenu
            var typeMenu = new MenuItem { Header = "Opening Type" };
            typeMenu.Items.Add(CreateCheckedMenuItem("Door", opening.OpeningType == OpeningTypes.Door,
                (s, e) => { opening.OpeningType = OpeningTypes.Door; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Gate", opening.OpeningType == OpeningTypes.Gate,
                (s, e) => { opening.OpeningType = OpeningTypes.Gate; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Hatch", opening.OpeningType == OpeningTypes.Hatch,
                (s, e) => { opening.OpeningType = OpeningTypes.Hatch; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Manhole", opening.OpeningType == OpeningTypes.Manhole,
                (s, e) => { opening.OpeningType = OpeningTypes.Manhole; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Aisle", opening.OpeningType == OpeningTypes.Aisle,
                (s, e) => { opening.OpeningType = OpeningTypes.Aisle; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Bay Entrance", opening.OpeningType == OpeningTypes.BayEntrance,
                (s, e) => { opening.OpeningType = OpeningTypes.BayEntrance; MarkDirty(); Redraw(); }));
            typeMenu.Items.Add(CreateCheckedMenuItem("Emergency Exit", opening.OpeningType == OpeningTypes.EmergencyExit,
                (s, e) => { opening.OpeningType = OpeningTypes.EmergencyExit; MarkDirty(); Redraw(); }));
            CanvasContextMenu.Items.Add(typeMenu);

            // State submenu
            var stateMenu = new MenuItem { Header = "State" };
            stateMenu.Items.Add(CreateCheckedMenuItem("Open", opening.State == OpeningStates.Open,
                (s, e) => { opening.State = OpeningStates.Open; MarkDirty(); Redraw(); }));
            stateMenu.Items.Add(CreateCheckedMenuItem("Closed", opening.State == OpeningStates.Closed,
                (s, e) => { opening.State = OpeningStates.Closed; MarkDirty(); Redraw(); }));
            stateMenu.Items.Add(CreateCheckedMenuItem("Locked", opening.State == OpeningStates.Locked,
                (s, e) => { opening.State = OpeningStates.Locked; MarkDirty(); Redraw(); }));
            stateMenu.Items.Add(CreateCheckedMenuItem("Emergency", opening.State == OpeningStates.Emergency,
                (s, e) => { opening.State = OpeningStates.Emergency; MarkDirty(); Redraw(); }));
            CanvasContextMenu.Items.Add(stateMenu);

            CanvasContextMenu.Items.Add(new Separator());

            CanvasContextMenu.Items.Add(CreateMenuItem("Delete", (s, e) => {
                SaveUndoState();
                _layout.Openings.Remove(opening);
                MarkDirty();
                Redraw();
                StatusText.Text = $"Deleted opening '{opening.Name}'";
            }));
        }

        #endregion
    }
}
