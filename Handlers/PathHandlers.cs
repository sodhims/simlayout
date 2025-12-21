using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Path Connection Mode Fields

        /// <summary>
        /// Current connection mode: single, 1:N, or chain
        /// </summary>
        private string _connectionMode = ConnectionModes.Single;

        /// <summary>
        /// Primary node for 1:N mode (the "1" in 1:N)
        /// </summary>
        private string? _primaryNodeId;

        /// <summary>
        /// Previous node for chain mode (to link from)
        /// </summary>
        private string? _chainPreviousNodeId;

        #endregion

        #region Path Connection Mode Operations

        /// <summary>
        /// Toggle connection mode: Single → 1:N → Chain → Single
        /// Press 'Q' while in path mode to cycle
        /// </summary>
        private void ToggleConnectionMode()
        {
            // Reset state when changing modes
            ResetConnectionModeState();

            _connectionMode = ConnectionModes.GetNextMode(_connectionMode);
            UpdateConnectionModeStatus();
        }

        /// <summary>
        /// Set specific connection mode
        /// </summary>
        private void SetConnectionMode(string mode)
        {
            ResetConnectionModeState();
            _connectionMode = mode;
            UpdateConnectionModeStatus();
        }

        private void UpdateConnectionModeStatus()
        {
            var modeName = ConnectionModes.GetDisplayName(_connectionMode);
            StatusText.Text = $"Path mode: {modeName} - Click source node";
            if (ModeText != null) ModeText.Text = $"Path: {modeName}";
        }

        /// <summary>
        /// Handle node click during path drawing based on current mode
        /// </summary>
        private void HandlePathNodeClick(string clickedNodeId)
        {
            switch (_connectionMode)
            {
                case ConnectionModes.Single:
                    HandleSingleModeClick(clickedNodeId);
                    break;

                case ConnectionModes.OneToMany:
                    HandleOneToManyModeClick(clickedNodeId);
                    break;

                case ConnectionModes.Chain:
                    HandleChainModeClick(clickedNodeId);
                    break;

                default:
                    HandleSingleModeClick(clickedNodeId);
                    break;
            }
        }

        /// <summary>
        /// Single mode: A → B creates path, done
        /// </summary>
        private void HandleSingleModeClick(string clickedNodeId)
        {
            if (_pathStartNodeId == null)
            {
                // First click - set start node
                _pathStartNodeId = clickedNodeId;
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == clickedNodeId);
                StatusText.Text = $"Click target node (from {node?.Name ?? "?"})";
            }
            else if (clickedNodeId != _pathStartNodeId)
            {
                // Second click - create path and finish
                CreatePath(_pathStartNodeId, clickedNodeId);
                _pathStartNodeId = null;
                _pathRenderer.ClearTempPath(EditorCanvas);
                StatusText.Text = "Path created. Click source for next path, or press ESC";
            }
        }

        /// <summary>
        /// 1:N mode: First click sets primary, subsequent clicks create paths from primary
        /// </summary>
        private void HandleOneToManyModeClick(string clickedNodeId)
        {
            if (_primaryNodeId == null)
            {
                // First click - set primary node (the "1" in 1:N)
                _primaryNodeId = clickedNodeId;
                _pathStartNodeId = clickedNodeId;
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == clickedNodeId);
                StatusText.Text = $"1:N Primary={node?.Name ?? "?"} - Click targets (ESC to finish)";
            }
            else if (clickedNodeId != _primaryNodeId)
            {
                // Subsequent clicks - create path from primary to clicked node
                CreatePath(_primaryNodeId, clickedNodeId);
                // Keep primary set for more connections
                var primaryNode = _layout.Nodes.FirstOrDefault(n => n.Id == _primaryNodeId);
                var targetNode = _layout.Nodes.FirstOrDefault(n => n.Id == clickedNodeId);
                StatusText.Text = $"1:N: {primaryNode?.Name}→{targetNode?.Name} - Click more or ESC";
            }
        }

        /// <summary>
        /// Chain mode: A → B → C → D creates A→B, B→C, C→D
        /// </summary>
        private void HandleChainModeClick(string clickedNodeId)
        {
            if (_chainPreviousNodeId == null)
            {
                // First click - set start of chain
                _chainPreviousNodeId = clickedNodeId;
                _pathStartNodeId = clickedNodeId;
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == clickedNodeId);
                StatusText.Text = $"Chain: Start={node?.Name ?? "?"} - Click next node";
            }
            else if (clickedNodeId != _chainPreviousNodeId)
            {
                // Create path from previous to current
                CreatePath(_chainPreviousNodeId, clickedNodeId);
                
                // Current becomes previous for next link
                _chainPreviousNodeId = clickedNodeId;
                _pathStartNodeId = clickedNodeId;
                
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == clickedNodeId);
                StatusText.Text = $"Chain: ...→{node?.Name ?? "?"} - Click next or ESC";
            }
        }

        /// <summary>
        /// Reset connection mode state (called on ESC or tool change)
        /// </summary>
        private void ResetConnectionModeState()
        {
            _pathStartNodeId = null;
            _primaryNodeId = null;
            _chainPreviousNodeId = null;
            _pathRenderer.ClearTempPath(EditorCanvas);
        }

        #endregion

        #region Path Operations

        private void CreatePath(string fromNodeId, string toNodeId)
        {
            // Check if path already exists
            var existing = _layout.Paths.FirstOrDefault(p =>
                (p.From == fromNodeId && p.To == toNodeId) ||
                (p.From == toNodeId && p.To == fromNodeId));

            if (existing != null)
            {
                StatusText.Text = "Path already exists between these nodes";
                return;
            }

            SaveUndoState();

            // Use direct routing by default
            var routingMode = RoutingModes.Direct;

            var path = new PathData
            {
                Id = Guid.NewGuid().ToString(),
                From = fromNodeId,
                To = toNodeId,
                PathType = PathTypes.Single,
                RoutingMode = routingMode,
                Visual = new PathVisual { Color = "#666666" },
                Simulation = new PathSimulation
                {
                    TransportType = TransportTypes.Conveyor,
                    Speed = 1.0,
                    Capacity = 10
                }
            };

            // Calculate distance
            var fromNode = _layout.Nodes.FirstOrDefault(n => n.Id == fromNodeId);
            var toNode = _layout.Nodes.FirstOrDefault(n => n.Id == toNodeId);

            if (fromNode != null && toNode != null)
            {
                var dx = toNode.Visual.X - fromNode.Visual.X;
                var dy = toNode.Visual.Y - fromNode.Visual.Y;
                path.Simulation.Distance = Math.Sqrt(dx * dx + dy * dy);
            }

            _layout.Paths.Add(path);

            _selectionService.SelectPath(path.Id);

            MarkDirty();
            RefreshAll();
            
            // Only show "Created path" message in single mode
            if (_connectionMode == ConnectionModes.Single)
            {
                StatusText.Text = $"Created path: {fromNode?.Name} → {toNode?.Name}";
            }
        }

        private void DeletePath(string pathId)
        {
            var path = _layout.Paths.FirstOrDefault(p => p.Id == pathId);
            if (path == null) return;

            SaveUndoState();
            _layout.Paths.Remove(path);

            _selectionService.ClearSelection();

            MarkDirty();
            RefreshAll();
            StatusText.Text = "Path deleted";
        }

        private void UpdateTempPath(Point currentPos)
        {
            // Determine which node to draw from based on mode
            string? sourceNodeId = _connectionMode switch
            {
                ConnectionModes.OneToMany => _primaryNodeId,
                ConnectionModes.Chain => _chainPreviousNodeId,
                _ => _pathStartNodeId
            };

            if (sourceNodeId == null) return;

            var startNode = _layout.Nodes.FirstOrDefault(n => n.Id == sourceNodeId);
            if (startNode == null) return;

            Point startPoint;
            
            // Check if starting node is inside a cell - if so, draw from cell's output terminal
            var cell = _layout.Groups.FirstOrDefault(g => g.IsCell && g.Members.Contains(sourceNodeId));
            if (cell != null)
            {
                // Get cell bounds
                var cellNodes = cell.Members.Select(id => _layout.Nodes.FirstOrDefault(n => n.Id == id))
                    .Where(n => n != null).ToList();
                if (cellNodes.Count > 0)
                {
                    const double pad = 15;
                    const double terminalOffset = 12;
                    double minX = cellNodes.Min(n => n!.Visual.X) - pad;
                    double minY = cellNodes.Min(n => n!.Visual.Y) - pad;
                    double maxX = cellNodes.Max(n => n!.Visual.X + n!.Visual.Width) + pad;
                    double maxY = cellNodes.Max(n => n!.Visual.Y + n!.Visual.Height) + pad;
                    
                    // Get output terminal position
                    startPoint = cell.OutputTerminalPosition?.ToLower() switch
                    {
                        "top" => new Point((minX + maxX) / 2, minY - terminalOffset),
                        "bottom" => new Point((minX + maxX) / 2, maxY + terminalOffset),
                        "left" => new Point(minX - terminalOffset, (minY + maxY) / 2),
                        _ => new Point(maxX + terminalOffset, (minY + maxY) / 2) // right default
                    };
                }
                else
                {
                    // Fallback to node terminal
                    startPoint = Services.TerminalHelper.GetNodeOutputTerminal(startNode);
                }
            }
            else
            {
                // Start from node's OUTPUT terminal
                startPoint = Services.TerminalHelper.GetNodeOutputTerminal(startNode);
            }

            _pathRenderer.ClearTempPath(EditorCanvas);
            _pathRenderer.DrawTempPath(EditorCanvas, startPoint, currentPos);
        }

        private void CancelPathDrawing()
        {
            ResetConnectionModeState();
            _isDrawingPath = false;
            StatusText.Text = "Path drawing cancelled";
        }

        private void StartPathDrawing_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "path";
            _isDrawingPath = true;
            ResetConnectionModeState();
            UpdateConnectionModeStatus();
        }

        #endregion
    }
}
