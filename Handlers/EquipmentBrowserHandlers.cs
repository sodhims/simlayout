using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Dialogs;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for Equipment Browser Panel events
    /// </summary>
    public partial class MainWindow
    {
        private void EquipmentBrowser_ItemSelected(object? sender, object item)
        {
            // When an item is selected in the browser, select it on the canvas and center on it
            _selectionService.ClearSelection();

            // Show blinking indicator on the selected item
            _selectionIndicator?.ShowIndicator(item);

            switch (item)
            {
                case NodeData node:
                    _selectionService.SelectNode(node.Id);
                    CenterOnElement(node.Visual?.X ?? 0, node.Visual?.Y ?? 0);
                    StatusText.Text = $"Selected node: {node.Name}";
                    break;

                case EOTCraneData crane:
                    // EOT cranes don't have direct selection - just center on them
                    var craneRunway = _layout.Runways.FirstOrDefault(r => r.Id == crane.RunwayId);
                    if (craneRunway != null)
                    {
                        double centerX = (craneRunway.StartX + craneRunway.EndX) / 2;
                        double centerY = (craneRunway.StartY + craneRunway.EndY) / 2;
                        CenterOnElement(centerX, centerY);
                    }
                    StatusText.Text = $"Selected EOT crane: {crane.Name}";
                    break;

                case JibCraneData jib:
                    CenterOnElement(jib.CenterX, jib.CenterY);
                    StatusText.Text = $"Selected jib crane: {jib.Name}";
                    break;

                case ZoneData zone:
                    CenterOnElement(zone.X + zone.Width / 2, zone.Y + zone.Height / 2);
                    StatusText.Text = $"Selected zone: {zone.Name}";
                    break;

                case AGVStationData station:
                    CenterOnElement(station.X, station.Y);
                    StatusText.Text = $"Selected AGV station: {station.Name}";
                    break;

                case ConveyorData conveyor:
                    if (conveyor.Path?.Count > 0)
                    {
                        CenterOnElement(conveyor.Path[0].X, conveyor.Path[0].Y);
                    }
                    StatusText.Text = $"Selected conveyor: {conveyor.Name}";
                    break;

                case OpeningData opening:
                    CenterOnElement(opening.X, opening.Y);
                    StatusText.Text = $"Selected opening: {opening.Name}";
                    break;

                case PathData path:
                    _selectionService.SelectPath(path.Id);
                    var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                    if (fromNode?.Visual != null)
                    {
                        CenterOnElement(fromNode.Visual.X, fromNode.Visual.Y);
                    }
                    StatusText.Text = $"Selected path";
                    break;

                case RunwayData rwy:
                    CenterOnElement((rwy.StartX + rwy.EndX) / 2, (rwy.StartY + rwy.EndY) / 2);
                    StatusText.Text = $"Selected runway: {rwy.Name}";
                    break;

                case WallData wall:
                    _selectionService.SelectWall(wall.Id);
                    CenterOnElement((wall.X1 + wall.X2) / 2, (wall.Y1 + wall.Y2) / 2);
                    StatusText.Text = $"Selected wall";
                    break;
            }

            Redraw();
        }

        private void EquipmentBrowser_ItemDoubleClicked(object? sender, object item)
        {
            // Double-click opens the properties dialog
            OpenPropertiesDialog(item);
        }

        private void EquipmentBrowser_EditRequested(object? sender, object item)
        {
            OpenPropertiesDialog(item);
        }

        private void OpenPropertiesDialog(object item)
        {
            bool updated = false;

            switch (item)
            {
                case EOTCraneData crane:
                    var craneDialog = new EOTCranePropertiesDialog(crane, _layout);
                    craneDialog.Owner = this;
                    if (craneDialog.ShowDialog() == true)
                        updated = true;
                    break;

                case ZoneData zone:
                    var zoneDialog = new ZonePropertiesDialog(zone);
                    zoneDialog.Owner = this;
                    if (zoneDialog.ShowDialog() == true)
                        updated = true;
                    break;

                case AGVStationData station:
                    var stationDialog = new AGVStationPropertiesDialog(station);
                    stationDialog.Owner = this;
                    if (stationDialog.ShowDialog() == true)
                        updated = true;
                    break;

                case ConveyorData conveyor:
                    var conveyorDialog = new ConveyorPropertiesDialog(conveyor);
                    conveyorDialog.Owner = this;
                    if (conveyorDialog.ShowDialog() == true)
                        updated = true;
                    break;

                case OpeningData opening:
                    var openingDialog = new OpeningPropertiesDialog(opening);
                    openingDialog.Owner = this;
                    if (openingDialog.ShowDialog() == true)
                        updated = true;
                    break;

                case NodeData node:
                    // Select the node and let the existing property system handle it
                    _selectionService.SelectNode(node.Id);
                    Redraw();
                    StatusText.Text = $"Selected node: {node.Name} - use right-click for properties";
                    return; // Don't mark as updated since we're just selecting

                default:
                    MessageBox.Show($"No properties dialog available for {item.GetType().Name}",
                        "Properties", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }

            if (updated)
            {
                MarkDirty();
                Redraw();
                EquipmentBrowserPanel?.RefreshTree();
                StatusText.Text = "Properties updated";
            }
        }

        private void CenterOnElement(double x, double y)
        {
            // Calculate scroll position to center on the element
            var scrollViewerWidth = CanvasScroller.ViewportWidth;
            var scrollViewerHeight = CanvasScroller.ViewportHeight;

            double targetScrollX = (x * _currentZoom) - (scrollViewerWidth / 2);
            double targetScrollY = (y * _currentZoom) - (scrollViewerHeight / 2);

            // Clamp to valid scroll range
            targetScrollX = Math.Max(0, Math.Min(targetScrollX, CanvasScroller.ScrollableWidth));
            targetScrollY = Math.Max(0, Math.Min(targetScrollY, CanvasScroller.ScrollableHeight));

            CanvasScroller.ScrollToHorizontalOffset(targetScrollX);
            CanvasScroller.ScrollToVerticalOffset(targetScrollY);
        }

        /// <summary>
        /// Refresh the equipment browser when layout changes
        /// </summary>
        private void RefreshEquipmentBrowser()
        {
            EquipmentBrowserPanel?.RefreshTree();
        }

        private void EquipmentBrowser_DataChanged(object? sender, EventArgs e)
        {
            // When data is edited in the grid, mark layout as dirty and redraw
            MarkDirty();
            Redraw();
            StatusText.Text = "Equipment data updated";
        }
    }
}
