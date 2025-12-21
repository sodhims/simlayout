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
        public string? TerminalType { get; set; }  // "input" or "output"
        public LayerType Layer { get; set; }
        public bool IsEditable { get; set; } = true;
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
        PrimaryAisle,
        RestrictedArea,
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
            // Test in reverse Z-order (top layers first) - Pedestrian down to Infrastructure
            // This ensures that elements on higher layers are selected before lower layers

            // In frictionless mode, skip terminals (not constrained entities)
            if (!frictionlessMode)
            {
                // Check node terminals first (highest priority for path connections)
                var terminalHit = HitTestNodeTerminals(layout, point, layerManager);
                if (terminalHit != null)
                    return terminalHit;
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

            // Check Spatial layer elements
            if (layerManager == null || layerManager.IsVisible(LayerType.Spatial))
            {
                bool spatialEditable = layerManager?.IsEditable(LayerType.Spatial) ?? true;

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

                // Check zones
                var zone = HitTestZones(layout, point);
                if (zone != null)
                {
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

                // Check runways
                var runway = HitTestRunways(layout, point);
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

        private RunwayData? HitTestRunways(LayoutData layout, Point point)
        {
            const double hitMargin = 6.0;
            foreach (var runway in layout.Runways)
            {
                var p1 = new Point(runway.StartX, runway.StartY);
                var p2 = new Point(runway.EndX, runway.EndY);
                if (DistanceToLine(point, p1, p2) < hitMargin)
                    return runway;
            }
            return null;
        }

        private ZoneData? HitTestZones(LayoutData layout, Point point)
        {
            foreach (var zone in layout.Zones)
            {
                var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);
                if (rect.Contains(point))
                    return zone;
            }
            return null;
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
