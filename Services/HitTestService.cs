using System;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    public class HitTestResult
    {
        public HitType Type { get; set; }
        public string? Id { get; set; }
        public NodeData? Node { get; set; }
        public PathData? Path { get; set; }
        public GroupData? Group { get; set; }
        public WallData? Wall { get; set; }
        public ColumnData? Column { get; set; }
        public OpeningData? Opening { get; set; }
        public ZoneData? Zone { get; set; }
        public RunwayData? Runway { get; set; }
        public PrimaryAisleData? PrimaryAisle { get; set; }
        public RestrictedAreaData? RestrictedArea { get; set; }
        public EOTCraneData? EOTCrane { get; set; }
        public JibCraneData? JibCrane { get; set; }
        public AGVWaypointData? AGVWaypoint { get; set; }
        public AGVStationData? AGVStation { get; set; }
        public string? TerminalType { get; set; }  // "input" or "output"
        public LayerType Layer { get; set; }
        public bool IsEditable { get; set; } = true;
        public int VertexIndex { get; set; } = -1;  // For zone vertex hit testing
    }

    public enum HitType
    {
        None,
        Node,
        NodeTerminal,
        Path,
        GroupBorder,
        CellTerminal,
        CellInterior,
        Zone,
        Wall,
        Column,
        Opening,
        Runway,
        EOTCrane,
        JibCrane,
        AGVWaypoint,
        AGVStation,
        PrimaryAisle,
        RestrictedArea,
        ZoneVertex,
        Canvas
    }

    public class HitTestService
    {
        /// <summary>
        /// Legacy hit test method (layer-unaware)
        /// </summary>
        public HitTestResult HitTest(LayoutData layout, Point point)
        {
            return HitTest(layout, point, null);
        }

        /// <summary>
        /// Layer-aware hit test method - respects visibility and editability
        /// </summary>
        public HitTestResult HitTest(LayoutData layout, Point point, ArchitectureLayerManager? layerManager)
        {
            return HitTest(layout, point, layerManager, false);
        }

        /// <summary>
        /// Layer-aware hit test method with frictionless mode support
        /// </summary>
        public HitTestResult HitTest(LayoutData layout, Point point, ArchitectureLayerManager? layerManager, bool frictionlessMode)
        {
            return HitTest(layout, point, layerManager, frictionlessMode, false);
        }

        /// <summary>
        /// Layer-aware hit test method with frictionless mode and design mode support
        /// </summary>
        public HitTestResult HitTest(LayoutData layout, Point point, ArchitectureLayerManager? layerManager, bool frictionlessMode, bool designMode)
        {
            DebugLogger.Log($"=== HitTest START at ({point.X:F1}, {point.Y:F1}), frictionless={frictionlessMode}, design={designMode} ===");

            // Test in reverse Z-order (top layers first) - Pedestrian down to Infrastructure
            // This ensures that elements on higher layers are selected before lower layers

            // In frictionless mode, skip terminals (not constrained entities)
            if (!frictionlessMode)
            {
                DebugLogger.Log("Checking node terminals...");
                // Check node terminals first (highest priority for path connections)
                var terminalHit = HitTestNodeTerminals(layout, point, layerManager);
                if (terminalHit != null)
                {
                    DebugLogger.Log($"HIT: NodeTerminal");
                    return terminalHit;
                }
            }

            // In frictionless mode, skip cell terminals and nodes (not constrained)
            if (!frictionlessMode)
            {
                // Check cell terminals
                var cellTerminalHit = HitTestCellTerminals(layout, point);
                if (cellTerminalHit != null)
                    return cellTerminalHit;

                // Check nodes (Equipment layer)
                if (layerManager == null || layerManager.IsVisible(LayerType.Equipment))
                {
                    var node = HitTestNodes(layout, point);
                    if (node != null)
                    {
                        bool editable = layerManager?.IsEditable(LayerType.Equipment) ?? true;
                        return new HitTestResult
                        {
                            Type = HitType.Node,
                            Id = node.Id,
                            Node = node,
                            Layer = LayerType.Equipment,
                            IsEditable = editable
                        };
                    }
                }

                // Check group borders
                var group = HitTestGroupBorders(layout, point);
                if (group != null)
                    return new HitTestResult { Type = HitType.GroupBorder, Id = group.Id, Group = group };

                // Check cell interiors
                var cell = HitTestCellInterior(layout, point);
                if (cell != null)
                    return new HitTestResult { Type = HitType.CellInterior, Id = cell.Id, Group = cell };
            }

            // Check paths (LocalFlow layer)
            if (layerManager == null || layerManager.IsVisible(LayerType.LocalFlow))
            {
                var path = HitTestPaths(layout, point);
                if (path != null)
                {
                    bool editable = layerManager?.IsEditable(LayerType.LocalFlow) ?? true;
                    return new HitTestResult
                    {
                        Type = HitType.Path,
                        Id = path.Id,
                        Path = path,
                        Layer = LayerType.LocalFlow,
                        IsEditable = editable
                    };
                }
            }

            // In design mode OR frictionless mode, check EOT cranes BEFORE zones
            // This allows clicking anywhere on the crane's runway zone to select/animate the crane
            // even if a zone is underneath
            if ((designMode || frictionlessMode) && (layerManager == null || layerManager.IsVisible(LayerType.OverheadTransport)))
            {
                bool overheadEditable = layerManager?.IsEditable(LayerType.OverheadTransport) ?? true;
                var eotCrane = HitTestEOTCranes(layout, point);
                if (eotCrane != null)
                {
                    string modeDesc = designMode ? "design mode" : "frictionless mode";
                    DebugLogger.Log($"HIT: EOT crane '{eotCrane.Name}' ({modeDesc} priority)");
                    return new HitTestResult
                    {
                        Type = HitType.EOTCrane,
                        Id = eotCrane.Id,
                        EOTCrane = eotCrane,
                        Layer = LayerType.OverheadTransport,
                        IsEditable = overheadEditable
                    };
                }

                // Also check jib cranes in frictionless mode for animation
                if (frictionlessMode)
                {
                    var jibCrane = HitTestJibCranes(layout, point, false);
                    if (jibCrane != null)
                    {
                        DebugLogger.Log($"HIT: Jib crane '{jibCrane.Name}' (frictionless mode priority)");
                        return new HitTestResult
                        {
                            Type = HitType.JibCrane,
                            Id = jibCrane.Id,
                            JibCrane = jibCrane,
                            Layer = LayerType.OverheadTransport,
                            IsEditable = overheadEditable
                        };
                    }
                }
            }

            // Check Spatial layer elements
            if (layerManager == null || layerManager.IsVisible(LayerType.Spatial))
            {
                bool spatialEditable = layerManager?.IsEditable(LayerType.Spatial) ?? true;

                // In design mode, check zone vertices for reshaping
                if (designMode)
                {
                    var (vertexZone, vertexIndex) = HitTestZoneVertices(layout, point);
                    if (vertexZone != null)
                    {
                        DebugLogger.Log($"HIT: Zone vertex {vertexIndex} of '{vertexZone.Name}'");
                        return new HitTestResult
                        {
                            Type = HitType.ZoneVertex,
                            Id = vertexZone.Id,
                            Zone = vertexZone,
                            VertexIndex = vertexIndex,
                            Layer = LayerType.Spatial,
                            IsEditable = spatialEditable
                        };
                    }
                }

                // Check restricted areas
                var restrictedArea = HitTestRestrictedAreas(layout, point);
                if (restrictedArea != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.RestrictedArea,
                        Id = restrictedArea.Id,
                        RestrictedArea = restrictedArea,
                        Layer = LayerType.Spatial,
                        IsEditable = spatialEditable
                    };
                }

                // Check primary aisles
                var aisle = HitTestPrimaryAisles(layout, point);
                if (aisle != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.PrimaryAisle,
                        Id = aisle.Id,
                        PrimaryAisle = aisle,
                        Layer = LayerType.Spatial,
                        IsEditable = spatialEditable
                    };
                }

                // Check zone interiors (for moving whole zone)
                var zone = HitTestZones(layout, point);
                if (zone != null)
                {
                    DebugLogger.Log($"HIT: Zone interior '{zone.Name}'");
                    return new HitTestResult
                    {
                        Type = HitType.Zone,
                        Id = zone.Id,
                        Zone = zone,
                        Layer = LayerType.Spatial,
                        IsEditable = spatialEditable
                    };
                }
            }

            // Check Infrastructure layer elements (runways) BEFORE EOT cranes in design mode
            if (designMode && (layerManager == null || layerManager.IsVisible(LayerType.Infrastructure)))
            {
                bool infraEditable = layerManager?.IsEditable(LayerType.Infrastructure) ?? true;

                // In design mode, runways have priority over EOT cranes (easier to select bays)
                var runway = HitTestRunways(layout, point, designMode);
                if (runway != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.Runway,
                        Id = runway.Id,
                        Runway = runway,
                        Layer = LayerType.Infrastructure,
                        IsEditable = infraEditable
                    };
                }
            }

            // Check OverheadTransport layer elements (EOT cranes)
            if (layerManager == null || layerManager.IsVisible(LayerType.OverheadTransport))
            {
                bool overheadEditable = layerManager?.IsEditable(LayerType.OverheadTransport) ?? true;

                // Check EOT cranes (higher priority than runways in normal mode)
                var eotCrane = HitTestEOTCranes(layout, point);
                if (eotCrane != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.EOTCrane,
                        Id = eotCrane.Id,
                        EOTCrane = eotCrane,
                        Layer = LayerType.OverheadTransport,
                        IsEditable = overheadEditable
                    };
                }

                // Check Jib cranes
                var jibCrane = HitTestJibCranes(layout, point, designMode);
                if (jibCrane != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.JibCrane,
                        Id = jibCrane.Id,
                        JibCrane = jibCrane,
                        Layer = LayerType.OverheadTransport,
                        IsEditable = overheadEditable
                    };
                }

                // Check AGV waypoints (GuidedTransport layer) - only in frictionless mode
                if (frictionlessMode)
                {
                    bool guidedEditable = layerManager?.IsEditable(LayerType.GuidedTransport) ?? true;
                    var agvWaypoint = HitTestAGVWaypoints(layout, point);
                    if (agvWaypoint != null)
                    {
                        return new HitTestResult
                        {
                            Type = HitType.AGVWaypoint,
                            Id = agvWaypoint.Id,
                            AGVWaypoint = agvWaypoint,
                            Layer = LayerType.GuidedTransport,
                            IsEditable = guidedEditable
                        };
                    }
                }
            }

            // Check GuidedTransport layer elements (AGV stations)
            DebugLogger.Log($"Checking GuidedTransport layer, visible={layerManager == null || layerManager.IsVisible(LayerType.GuidedTransport)}");
            if (layerManager == null || layerManager.IsVisible(LayerType.GuidedTransport))
            {
                bool guidedEditable = layerManager?.IsEditable(LayerType.GuidedTransport) ?? true;

                DebugLogger.Log($"Checking AGV stations (count={layout.AGVStations.Count})...");
                // Check AGV stations (always visible, draggable in design mode)
                var agvStation = HitTestAGVStations(layout, point, designMode);
                if (agvStation != null)
                {
                    DebugLogger.Log($"HIT: AGV Station '{agvStation.Name}'");
                    return new HitTestResult
                    {
                        Type = HitType.AGVStation,
                        Id = agvStation.Id,
                        AGVStation = agvStation,
                        Layer = LayerType.GuidedTransport,
                        IsEditable = guidedEditable
                    };
                }
                DebugLogger.Log("No AGV station hit");
            }

            // Check Infrastructure layer elements
            if (layerManager == null || layerManager.IsVisible(LayerType.Infrastructure))
            {
                bool infraEditable = layerManager?.IsEditable(LayerType.Infrastructure) ?? true;

                // Check openings
                var opening = HitTestOpenings(layout, point);
                if (opening != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.Opening,
                        Id = opening.Id,
                        Opening = opening,
                        Layer = LayerType.Infrastructure,
                        IsEditable = infraEditable
                    };
                }

                // Check columns
                var column = HitTestColumns(layout, point);
                if (column != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.Column,
                        Id = column.Id,
                        Column = column,
                        Layer = LayerType.Infrastructure,
                        IsEditable = infraEditable
                    };
                }

                // Check walls
                var wall = HitTestWalls(layout, point);
                if (wall != null)
                {
                    return new HitTestResult
                    {
                        Type = HitType.Wall,
                        Id = wall.Id,
                        Wall = wall,
                        Layer = LayerType.Infrastructure,
                        IsEditable = infraEditable
                    };
                }

                // Check runways (if not already checked in design mode)
                if (!designMode)
                {
                    var runway = HitTestRunways(layout, point, false);
                    if (runway != null)
                    {
                        return new HitTestResult
                        {
                            Type = HitType.Runway,
                            Id = runway.Id,
                            Runway = runway,
                            Layer = LayerType.Infrastructure,
                            IsEditable = infraEditable
                        };
                    }
                }
            }

            System.Console.WriteLine("[DEBUG] === HitTest END: Nothing hit, returning Canvas ===");
            return new HitTestResult { Type = HitType.Canvas };
        }

        private HitTestResult? HitTestNodeTerminals(LayoutData layout, Point point)
        {
            return HitTestNodeTerminals(layout, point, null);
        }

        private HitTestResult? HitTestNodeTerminals(LayoutData layout, Point point, ArchitectureLayerManager? layerManager)
        {
            // Only test if Equipment layer is visible
            if (layerManager != null && !layerManager.IsVisible(LayerType.Equipment))
                return null;

            bool editable = layerManager?.IsEditable(LayerType.Equipment) ?? true;

            foreach (var node in layout.Nodes.Reverse<NodeData>())
            {
                // Use TerminalHelper for hit testing
                if (TerminalHelper.HasInputTerminal(node.Type) && TerminalHelper.HitTestInputTerminal(node, point))
                {
                    return new HitTestResult
                    {
                        Type = HitType.NodeTerminal,
                        Id = node.Id,
                        Node = node,
                        TerminalType = "input",
                        Layer = LayerType.Equipment,
                        IsEditable = editable
                    };
                }

                if (TerminalHelper.HasOutputTerminal(node.Type) && TerminalHelper.HitTestOutputTerminal(node, point))
                {
                    return new HitTestResult
                    {
                        Type = HitType.NodeTerminal,
                        Id = node.Id,
                        Node = node,
                        TerminalType = "output",
                        Layer = LayerType.Equipment,
                        IsEditable = editable
                    };
                }
            }
            return null;
        }

        private HitTestResult? HitTestCellTerminals(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.Where(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds == null) continue;

                var rect = bounds.Value;
                const double terminalOffset = 12;

                // Input terminal position
                var inputPos = GetTerminalPosition(rect, group.InputTerminalPosition, terminalOffset);
                if (Distance(point, inputPos) < RenderConstants.TerminalHitRadius)
                {
                    return new HitTestResult
                    {
                        Type = HitType.CellTerminal,
                        Id = group.Id,
                        Group = group,
                        TerminalType = "input"
                    };
                }

                // Output terminal position
                var outputPos = GetTerminalPosition(rect, group.OutputTerminalPosition, terminalOffset);
                if (Distance(point, outputPos) < RenderConstants.TerminalHitRadius)
                {
                    return new HitTestResult
                    {
                        Type = HitType.CellTerminal,
                        Id = group.Id,
                        Group = group,
                        TerminalType = "output"
                    };
                }
            }
            return null;
        }

        private Point GetTerminalPosition(Rect bounds, string position, double offset)
        {
            return position?.ToLower() switch
            {
                "top" => new Point(bounds.X + bounds.Width / 2, bounds.Y - offset),
                "bottom" => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height + offset),
                "right" => new Point(bounds.X + bounds.Width + offset, bounds.Y + bounds.Height / 2),
                _ => new Point(bounds.X - offset, bounds.Y + bounds.Height / 2)
            };
        }

        private NodeData? HitTestNodes(LayoutData layout, Point point)
        {
            foreach (var node in layout.Nodes.Reverse<NodeData>())
            {
                var rect = new Rect(
                    node.Visual.X - RenderConstants.NodeHitMargin, node.Visual.Y - RenderConstants.NodeHitMargin,
                    node.Visual.Width + RenderConstants.NodeHitMargin * 2, node.Visual.Height + RenderConstants.NodeHitMargin * 2);
                if (rect.Contains(point)) return node;
            }
            return null;
        }

        private PathData? HitTestPaths(LayoutData layout, Point point)
        {
            foreach (var path in layout.Paths)
            {
                var fromNode = layout.Nodes.FirstOrDefault(n => n.Id == path.From);
                var toNode = layout.Nodes.FirstOrDefault(n => n.Id == path.To);
                if (fromNode == null || toNode == null) continue;
                if (DistanceToLine(point, GetNodeCenter(fromNode), GetNodeCenter(toNode)) < RenderConstants.PathHitMargin)
                    return path;
            }
            return null;
        }

        private GroupData? HitTestGroupBorders(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups)
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds == null) continue;

                var rect = bounds.Value;
                var outerRect = new Rect(rect.X - RenderConstants.GroupBorderThickness, rect.Y - RenderConstants.GroupBorderThickness,
                    rect.Width + RenderConstants.GroupBorderThickness * 2, rect.Height + RenderConstants.GroupBorderThickness * 2);
                var innerRect = new Rect(rect.X + RenderConstants.GroupBorderThickness, rect.Y + RenderConstants.GroupBorderThickness,
                    rect.Width - RenderConstants.GroupBorderThickness * 2, rect.Height - RenderConstants.GroupBorderThickness * 2);

                if (outerRect.Contains(point) && !innerRect.Contains(point))
                    return group;
            }
            return null;
        }

        private GroupData? HitTestCellInterior(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.OrderByDescending(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds?.Contains(point) == true) return group;
            }
            return null;
        }

        public GroupData? HitTestCellAtPoint(LayoutData layout, Point point)
        {
            foreach (var group in layout.Groups.Where(g => g.IsCell))
            {
                if (group.Members.Count == 0) continue;
                var bounds = GetGroupBounds(layout, group);
                if (bounds?.Contains(point) == true) return group;
            }
            return null;
        }

        public Rect? GetGroupBounds(LayoutData layout, GroupData group)
        {
            var memberNodes = group.Members
                .Select(id => layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null).ToList();
            if (memberNodes.Count == 0) return null;

            double padding = 15.0;
            var minX = memberNodes.Min(n => n!.Visual.X) - padding;
            var minY = memberNodes.Min(n => n!.Visual.Y) - padding;
            var maxX = memberNodes.Max(n => n!.Visual.X + n!.Visual.Width) + padding;
            var maxY = memberNodes.Max(n => n!.Visual.Y + n!.Visual.Height) + padding;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private WallData? HitTestWalls(LayoutData layout, Point point)
        {
            const double hitMargin = 5.0;
            foreach (var wall in layout.Walls)
            {
                var p1 = new Point(wall.X1, wall.Y1);
                var p2 = new Point(wall.X2, wall.Y2);
                if (DistanceToLine(point, p1, p2) < hitMargin)
                    return wall;
            }
            return null;
        }

        private ColumnData? HitTestColumns(LayoutData layout, Point point)
        {
            foreach (var column in layout.Columns)
            {
                var rect = new Rect(
                    column.X - column.Width / 2,
                    column.Y - column.Height / 2,
                    column.Width,
                    column.Height);
                if (rect.Contains(point))
                    return column;
            }
            return null;
        }

        private OpeningData? HitTestOpenings(LayoutData layout, Point point)
        {
            foreach (var opening in layout.Openings)
            {
                double width = opening is ConstrainedOpening constrained ? constrained.Width :
                               opening is UnconstrainedOpening unconstrained ? unconstrained.Width : 36;
                double height = opening is ConstrainedOpening constrainedH ? constrainedH.Height : 20;

                var rect = new Rect(opening.X, opening.Y, width, height);
                if (rect.Contains(point))
                    return opening;
            }
            return null;
        }

        private RunwayData? HitTestRunways(LayoutData layout, Point point, bool designMode = false)
        {
            // In design mode, use a much larger hit margin to make narrow runways easier to click
            const double normalHitMargin = 6.0;
            const double designModeHitMargin = 20.0;  // Much larger for easier selection
            double hitMargin = designMode ? designModeHitMargin : normalHitMargin;

            foreach (var runway in layout.Runways)
            {
                var p1 = new Point(runway.StartX, runway.StartY);
                var p2 = new Point(runway.EndX, runway.EndY);
                if (DistanceToLine(point, p1, p2) < hitMargin)
                    return runway;
            }
            return null;
        }

        private AGVStationData? HitTestAGVStations(LayoutData layout, Point point, bool designMode = false)
        {
            // In design mode, use a larger hit radius to make stations easier to click
            const double normalHitRadius = 15.0;
            const double designModeHitRadius = 25.0;
            double hitRadius = designMode ? designModeHitRadius : normalHitRadius;

            DebugLogger.Log($"HitTestAGVStations at ({point.X:F1}, {point.Y:F1}), designMode={designMode}, radius={hitRadius}");
            DebugLogger.Log($"Checking {layout.AGVStations.Count} AGV stations");

            foreach (var station in layout.AGVStations)
            {
                double dx = point.X - station.X;
                double dy = point.Y - station.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                DebugLogger.Log($"  Station '{station.Name}' at ({station.X:F1}, {station.Y:F1}), distance={distance:F1}");

                if (distance <= hitRadius)
                {
                    DebugLogger.Log($"  HIT! Returning station '{station.Name}'");
                    return station;
                }
            }
            DebugLogger.Log("  No AGV station within hit radius");
            return null;
        }

        private EOTCraneData? HitTestEOTCranes(LayoutData layout, Point point)
        {
            // Use larger hit radius in frictionless mode for easier handle grabbing
            const double normalHitRadius = 12.0;
            const double frictionlessHitRadius = 20.0;
            double hitRadius = layout.FrictionlessMode ? frictionlessHitRadius : normalHitRadius;

            // In frictionless or design mode, allow clicking anywhere on the crane's runway zone
            bool allowRunwayZoneHit = layout.FrictionlessMode || layout.DesignMode;

            foreach (var crane in layout.EOTCranes)
            {
                // Find the runway
                var runway = layout.Runways?.FirstOrDefault(r => r.Id == crane.RunwayId);
                if (runway == null) continue;

                // Get crane's current position on runway
                var (craneX, craneY) = runway.GetPositionAt(crane.BridgePosition);

                // Check if click is within hit radius of bridge position
                var dx = point.X - craneX;
                var dy = point.Y - craneY;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= hitRadius)
                    return crane;

                // In frictionless/design mode, also allow clicking anywhere along the crane's zone on the runway
                if (allowRunwayZoneHit)
                {
                    // Get the crane's zone segment on the runway
                    var (zoneStartX, zoneStartY) = runway.GetPositionAt(crane.ZoneMin);
                    var (zoneEndX, zoneEndY) = runway.GetPositionAt(crane.ZoneMax);

                    // Check if click is near the runway line within the crane's zone
                    double distToLine = DistanceToLineSegment(point,
                        new Point(zoneStartX, zoneStartY),
                        new Point(zoneEndX, zoneEndY));

                    // Use a wider hit margin for the runway zone (same as runway hit margin)
                    const double runwayZoneHitMargin = 15.0;
                    if (distToLine <= runwayZoneHitMargin)
                    {
                        DebugLogger.Log($"[HitTest] EOT crane '{crane.Name}' hit via runway zone, distance to line = {distToLine:F1}");
                        return crane;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Calculate distance from a point to a line segment
        /// </summary>
        private double DistanceToLineSegment(Point p, Point a, Point b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return Distance(p, a);

            // Calculate projection parameter
            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            var projX = a.X + t * dx;
            var projY = a.Y + t * dy;

            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        private JibCraneData? HitTestJibCranes(LayoutData layout, Point point, bool designMode = false)
        {
            // In design mode, use larger hit radius for easier selection
            const double normalHitRadius = 15.0;
            const double designModeHitRadius = 25.0;
            double hitRadius = designMode ? designModeHitRadius : normalHitRadius;

            if (layout.JibCranes == null) return null;

            foreach (var jib in layout.JibCranes)
            {
                // Check if click is within hit radius of jib center
                var dx = point.X - jib.CenterX;
                var dy = point.Y - jib.CenterY;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= hitRadius)
                    return jib;
            }
            return null;
        }

        private AGVWaypointData? HitTestAGVWaypoints(LayoutData layout, Point point)
        {
            const double hitRadius = 12.0; // Same as handle size

            foreach (var waypoint in layout.AGVWaypoints)
            {
                var dx = point.X - waypoint.X;
                var dy = point.Y - waypoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= hitRadius)
                    return waypoint;
            }
            return null;
        }

        private ZoneData? HitTestZones(LayoutData layout, Point point)
        {
            foreach (var zone in layout.Zones)
            {
                // Check if zone is polygon-based (Points) or rectangle-based (X/Y/Width/Height)
                if (zone.Points != null && zone.Points.Count >= 3)
                {
                    // Polygon hit test using point-in-polygon algorithm
                    if (IsPointInPolygon(point, zone.Points))
                        return zone;
                }
                else if (zone.Width > 0 && zone.Height > 0)
                {
                    // Rectangle hit test
                    var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);
                    if (rect.Contains(point))
                        return zone;
                }
            }
            return null;
        }

        /// <summary>
        /// Point-in-polygon test using ray casting algorithm
        /// </summary>
        private bool IsPointInPolygon(Point point, System.Collections.Generic.IList<PointData> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                double xi = polygon[i].X, yi = polygon[i].Y;
                double xj = polygon[j].X, yj = polygon[j].Y;

                if (((yi > point.Y) != (yj > point.Y)) &&
                    (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        /// <summary>
        /// Hit test zone vertices for reshaping in design mode
        /// </summary>
        private (ZoneData? zone, int vertexIndex) HitTestZoneVertices(LayoutData layout, Point point)
        {
            const double vertexHitRadius = 20.0;  // Large hit radius for easy vertex grabbing

            foreach (var zone in layout.Zones)
            {
                if (zone.Points == null || zone.Points.Count == 0)
                    continue;

                for (int i = 0; i < zone.Points.Count; i++)
                {
                    var vertex = zone.Points[i];
                    double dist = Math.Sqrt(
                        (point.X - vertex.X) * (point.X - vertex.X) +
                        (point.Y - vertex.Y) * (point.Y - vertex.Y));

                    if (dist <= vertexHitRadius)
                    {
                        return (zone, i);
                    }
                }
            }

            return (null, -1);
        }

        private PrimaryAisleData? HitTestPrimaryAisles(LayoutData layout, Point point)
        {
            foreach (var aisle in layout.PrimaryAisles)
            {
                if (aisle.Centerline.Count < 2)
                    continue;

                // Check if point is near the aisle centerline within the aisle width
                double halfWidth = aisle.Width / 2;
                for (int i = 0; i < aisle.Centerline.Count - 1; i++)
                {
                    var p1 = new Point(aisle.Centerline[i].X, aisle.Centerline[i].Y);
                    var p2 = new Point(aisle.Centerline[i + 1].X, aisle.Centerline[i + 1].Y);
                    if (DistanceToLine(point, p1, p2) < halfWidth)
                        return aisle;
                }
            }
            return null;
        }

        private RestrictedAreaData? HitTestRestrictedAreas(LayoutData layout, Point point)
        {
            foreach (var area in layout.RestrictedAreas)
            {
                var rect = new Rect(area.X, area.Y, area.Width, area.Height);
                if (rect.Contains(point))
                    return area;
            }
            return null;
        }

        private Point GetNodeCenter(NodeData node) =>
            new Point(node.Visual.X + node.Visual.Width / 2, node.Visual.Y + node.Visual.Height / 2);

        private double Distance(Point a, Point b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private double DistanceToLine(Point p, Point a, Point b)
        {
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;
            if (lengthSq == 0) return Distance(p, a);
            var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            var projX = a.X + t * dx; var projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }
    }
}
