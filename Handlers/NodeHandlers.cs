using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Node Operations

        private string? _pendingNodeType;
        private string? _pathStartNodeId;
        
        // Auto-connect snap distance (in pixels) - terminals within this range will attract
        private const double AutoConnectSnapDistance = 50.0;

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string nodeType)
            {
                _pendingNodeType = nodeType;
                EditorCanvas.Cursor = System.Windows.Input.Cursors.Cross;
                StatusText.Text = $"Click on canvas to place {nodeType}";
                if (ModeText != null) ModeText.Text = $"Mode: Place {nodeType}";
            }
        }

        private void AddNodeHere_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string nodeType)
                PlaceNodeAtPosition(nodeType, _contextMenuPosition);
        }

        private void PlaceNodeAtPosition(string nodeType, Point pos)
        {
            SaveUndoState();
            
            // Create node at approximate position first
            var snapped = SnapToGrid(pos);
            var node = LayoutFactory.CreateNode(nodeType, snapped.X - 40, snapped.Y - 30, _layout);
            
            // ═══════════════════════════════════════════════════════════════
            // TERMINAL ATTRACT MODE: Snap terminals together before placement
            // ═══════════════════════════════════════════════════════════════
            var attraction = FindTerminalAttraction(node);
            
            if (attraction.HasValue)
            {
                // Move node so terminals overlap exactly
                node.Visual.X += attraction.Value.offset.X;
                node.Visual.Y += attraction.Value.offset.Y;
            }
            
            _layout.Nodes.Add(node);
            
            // Auto-connect if terminals were attracted
            int connectionsCreated = 0;
            if (attraction.HasValue)
            {
                var target = attraction.Value.targetNode;
                if (attraction.Value.connectionDir == "incoming")
                {
                    // Existing node feeds into new node (existing.exit → new.entry)
                    if (!PathExists(target.Id, node.Id))
                    {
                        CreateAutoPath(target, node, "output", "input");
                        connectionsCreated++;
                    }
                }
                else // "outgoing"
                {
                    // New node feeds into existing node (new.exit → existing.entry)
                    if (!PathExists(node.Id, target.Id))
                    {
                        CreateAutoPath(node, target, "output", "input");
                        connectionsCreated++;
                    }
                }
            }
            
            _selectionService.SelectNode(node.Id);
            MarkDirty();
            RefreshAll();
            
            if (connectionsCreated > 0)
            {
                StatusText.Text = $"Added {node.Name} (snapped & connected)";
            }
            else
            {
                StatusText.Text = $"Added {node.Name}";
            }
        }

        /// <summary>
        /// Find the best terminal attraction for a new node.
        /// Returns the offset to move the node so terminals overlap, plus connection info.
        /// </summary>
        private (Vector offset, NodeData targetNode, string connectionDir)? FindTerminalAttraction(NodeData newNode)
        {
            var newTerminals = GetNodeTerminalPositions(newNode);
            if (newTerminals == null) return null;
            
            var (newEntryPos, newExitPos) = newTerminals.Value;
            
            double bestDistance = AutoConnectSnapDistance;
            (Vector offset, NodeData targetNode, string connectionDir)? bestAttraction = null;
            
            foreach (var existingNode in _layout.Nodes.Where(n => n.Id != newNode.Id))
            {
                var existingTerminals = GetNodeTerminalPositions(existingNode);
                if (existingTerminals == null) continue;
                
                var (existingEntryPos, existingExitPos) = existingTerminals.Value;
                
                // Case 1: New node's ENTRY near existing node's EXIT
                // → Snap new node so its entry overlaps existing exit
                if (newEntryPos.HasValue && existingExitPos.HasValue)
                {
                    var distance = (newEntryPos.Value - existingExitPos.Value).Length;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        // Calculate offset to move new node so terminals overlap
                        var offset = existingExitPos.Value - newEntryPos.Value;
                        bestAttraction = (offset, existingNode, "incoming");
                    }
                }
                
                // Case 2: New node's EXIT near existing node's ENTRY
                // → Snap new node so its exit overlaps existing entry
                if (newExitPos.HasValue && existingEntryPos.HasValue)
                {
                    var distance = (newExitPos.Value - existingEntryPos.Value).Length;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        // Calculate offset to move new node so terminals overlap
                        var offset = existingEntryPos.Value - newExitPos.Value;
                        bestAttraction = (offset, existingNode, "outgoing");
                    }
                }
            }
            
            return bestAttraction;
        }

        /// <summary>
        /// Get the world positions of a node's entry and exit terminals.
        /// Returns null if node has no terminals.
        /// </summary>
        private (Point? entryPos, Point? exitPos)? GetNodeTerminalPositions(NodeData node)
        {
            var visual = node.Visual;
            var terminalLayout = visual.TerminalLayout ?? TerminalLayouts.LeftRight;
            var (inputSide, outputSide) = TerminalLayouts.ParseLayout(terminalLayout);
            
            // Calculate node center
            double centerX = visual.X + visual.Width / 2;
            double centerY = visual.Y + visual.Height / 2;
            
            Point? entryPos = GetTerminalWorldPosition(visual, inputSide, centerX, centerY);
            Point? exitPos = GetTerminalWorldPosition(visual, outputSide, centerX, centerY);
            
            return (entryPos, exitPos);
        }

        /// <summary>
        /// Get world position for a terminal on a given side
        /// </summary>
        private Point? GetTerminalWorldPosition(NodeVisual visual, string side, double centerX, double centerY)
        {
            if (string.IsNullOrEmpty(side) || side == "center")
                return new Point(centerX, centerY);
            
            double halfWidth = visual.Width / 2;
            double halfHeight = visual.Height / 2;
            
            return side.ToLower() switch
            {
                "left" => new Point(visual.X, centerY),
                "right" => new Point(visual.X + visual.Width, centerY),
                "top" => new Point(centerX, visual.Y),
                "bottom" => new Point(centerX, visual.Y + visual.Height),
                _ => new Point(centerX, centerY)
            };
        }

        /// <summary>
        /// Check if a path already exists between two nodes
        /// </summary>
        private bool PathExists(string fromId, string toId)
        {
            return _layout.Paths.Any(p => p.From == fromId && p.To == toId);
        }

        /// <summary>
        /// Create an auto-generated path between two nodes
        /// </summary>
        private void CreateAutoPath(NodeData fromNode, NodeData toNode, string fromTerminal, string toTerminal)
        {
            var path = new PathData
            {
                Id = Guid.NewGuid().ToString(),
                From = fromNode.Id,
                To = toNode.Id,
                FromTerminal = fromTerminal,
                ToTerminal = toTerminal,
                PathType = PathTypes.Single,
                ConnectionType = ConnectionTypes.PartFlow,
                RoutingMode = _layout.Display?.PathRoutingDefault ?? RoutingModes.Direct,
                Visual = new PathVisual()
            };
            
            path.Visual.ApplyConnectionTypeStyle(path.ConnectionType);
            _layout.Paths.Add(path);
            
            Helpers.DebugLogger.Log($"Auto-connected: {fromNode.Name} → {toNode.Name}");
        }

        private void CancelNodePlacement()
        {
            _pendingNodeType = null;
            EditorCanvas.Cursor = System.Windows.Input.Cursors.Arrow;
            StatusText.Text = "Ready";
            if (ModeText != null) ModeText.Text = "Mode: Select";
        }

        private void DragSelectedNodes(Point currentPos)
        {
            // Check if frictionless mode is active
            if (_layout != null && _layout.FrictionlessMode)
            {
                // In frictionless mode, use constrained dragging
                DragConstrainedEntities(currentPos);
                return;
            }

            // Normal unconstrained dragging
            var delta = currentPos - _lastMousePos;
            var nodes = _selectionService.GetSelectedNodes(_layout);

            foreach (var node in nodes)
            {
                node.Visual.X += delta.X;
                node.Visual.Y += delta.Y;
            }

            MoveSelectedVisuals();  // Fast move, no full redraw

            // Update connecting path visuals so they follow nodes while dragging
            var movedIds = nodes.Select(n => n.Id).ToList();
            UpdatePathVisualsForMovedNodes(movedIds);
        }

        private void DragConstrainedEntities(Point currentPos)
        {
            if (_layout == null) return;

            var constrainedDragService = new LayoutEditor.Services.ConstrainedDragService(_layout);

            // For now, constrained dragging is a proof-of-concept
            // In future stages, we'll add proper selection tracking for constrained entities
            // Placeholder implementation demonstrates the constrained dragging mechanism

            // TODO: Add zone/crane selection tracking to SelectionService
            // For now, we just demonstrate the API is available

            StatusText.Text = "Frictionless mode: Constrained dragging active";

            // Redraw to show any updates
            Redraw();
        }

        private void FinishDrag()
        {
            // Snap to grid on drag finish
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (_isDragStarted && IsSnapGridEnabled())
            {
                foreach (var node in nodes)
                {
                    var snapped = SnapToGrid(new Point(node.Visual.X, node.Visual.Y));
                    node.Visual.X = snapped.X;
                    node.Visual.Y = snapped.Y;
                }
            }
            
            if (_isDragStarted && nodes.Any())
            {
                // Clear waypoints on paths connected to moved nodes so they re-route
                var movedIds = new HashSet<string>(nodes.Select(n => n.Id));
                
                // Debug log
                Helpers.DebugLogger.Log($"FinishDrag: {movedIds.Count} nodes moved: {string.Join(", ", movedIds)}");
                
                int clearedCount = 0;
                foreach (var path in _layout.Paths)
                {
                    if (movedIds.Contains(path.From) || movedIds.Contains(path.To))
                    {
                        Helpers.DebugLogger.Log($"  Clearing waypoints for path {path.Id}: From={path.From}, To={path.To}, had {path.Visual.Waypoints.Count} waypoints");
                        path.Visual.Waypoints.Clear();
                        clearedCount++;
                    }
                }
                Helpers.DebugLogger.Log($"  Cleared waypoints on {clearedCount} paths");
                
                MarkDirty();
            }
            
            // Always redraw after area selection to show wall/path selection highlights
            if (_isDrawingSelectionRect || _selectionService.HasSelection || HasWallSelection)
            {
                Redraw();
            }
            
            _isDragging = false;
            _isDragStarted = false;
            _isDrawingSelectionRect = false;  // Reset area selection flag
            ClearSelectionRectangle();
        }

        private Point SnapToGrid(Point point)
        {
            if (!IsSnapGridEnabled()) return point;
            int gridSize = _layout?.Canvas?.GridSize ?? 25;
            return new Point(
                Math.Round(point.X / gridSize) * gridSize,
                Math.Round(point.Y / gridSize) * gridSize);
        }

        #endregion
    }
}
