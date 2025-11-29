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
            var hitResult = _hitTestService.HitTest(_layout, position);

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

            CanvasContextMenu.Items.Add(CreateMenuItem("Edit Properties", (s, e) => {
                // Focus property panel
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

            CanvasContextMenu.Items.Add(CreateMenuItem("Edit Properties", (s, e) => {
                UpdatePropertyPanel();
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
            _selectionService.SelectGroup(group.Id, group.Members);
            UpdateSelectionVisuals();

            var headerItem = CreateMenuItem($"Group: {group.Name}", null);
            headerItem.IsEnabled = false;
            CanvasContextMenu.Items.Add(headerItem);
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

        #endregion
    }
}
