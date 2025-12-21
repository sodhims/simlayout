using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LayoutEditor.Models;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODir = System.IO.Directory;

namespace LayoutEditor.Services
{
    public class NodeRenderer
    {
        private readonly SelectionService _selection;
        private readonly Dictionary<string, BitmapImage> _iconCache = new();
        private readonly string _iconFolderPath;

        public NodeRenderer(SelectionService selection)
        {
            _selection = selection;
            _iconFolderPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeIcons");
            if (!IODir.Exists(_iconFolderPath))
                try { IODir.CreateDirectory(_iconFolderPath); } catch { }
        }

        public void RenderNodes(LayoutData layout, Canvas canvas, Action<string, UIElement> registerElement)
        {
            foreach (var node in layout.Nodes)
            {
                var element = CreateNodeElement(node);
                canvas.Children.Add(element);
                registerElement(node.Id, element);
            }
        }

        public void DrawNodes(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
            => RenderNodes(layout, canvas, registerElement);

        public UIElement CreateNodeElement(NodeData node)
        {
            var isSelected = _selection.IsNodeSelected(node.Id);
            var settings = EditorSettings.Instance;
            
            // Calculate terminal extensions based on positions
            var terminalExtensions = GetTerminalExtensions(node);
            
            // Extra height for label when terminal is at bottom (label goes on top)
            double labelExtension = 0;
            bool labelOnTop = node.Visual.OutputTerminalPosition?.ToLower() == "bottom" ||
                              node.Visual.InputTerminalPosition?.ToLower() == "bottom";
            if (labelOnTop)
            {
                labelExtension = 14;  // Space for label text above node
            }
            
            // Container includes space for terminals sticking out on all sides
            var container = new Canvas
            {
                Width = node.Visual.Width + terminalExtensions.Left + terminalExtensions.Right,
                Height = node.Visual.Height + terminalExtensions.Top + terminalExtensions.Bottom + labelExtension,
                Background = Brushes.Transparent
            };
            
            // Offset for node content when label is on top
            double topOffset = labelOnTop ? labelExtension : 0;

            // Main node rectangle (offset to allow terminal space)
            var background = new Rectangle
            {
                Width = node.Visual.Width,
                Height = node.Visual.Height,
                Fill = new SolidColorBrush(Color.FromArgb(80, 245, 245, 245)),
                Stroke = GetNodeBrush(node),
                StrokeThickness = isSelected ? settings.LineThickness + 1 : settings.LineThickness,
                RadiusX = 4, RadiusY = 4
            };

            if (isSelected)
            {
                background.Stroke = new SolidColorBrush(Colors.DodgerBlue);
                background.StrokeDashArray = new DoubleCollection { 4, 2 };
            }
            
            Canvas.SetLeft(background, terminalExtensions.Left);
            Canvas.SetTop(background, terminalExtensions.Top + topOffset);
            container.Children.Add(background);

            // Icon
            var icon = CreateIcon(node);
            if (icon != null)
            {
                Canvas.SetLeft(icon, terminalExtensions.Left + (node.Visual.Width - 28) / 2);
                Canvas.SetTop(icon, terminalExtensions.Top + topOffset + 4);
                container.Children.Add(icon);
            }

            // Badges
            if (IsMachineType(node.Type) && node.Simulation.Servers > 1)
            {
                var badge = CreateBadge($"x{node.Simulation.Servers}", Brushes.RoyalBlue);
                Canvas.SetLeft(badge, terminalExtensions.Left + node.Visual.Width - 18);
                Canvas.SetTop(badge, terminalExtensions.Top + topOffset + 2);
                container.Children.Add(badge);
            }
            if ((node.Type == "buffer" || node.Type == "storage") && node.Simulation.Capacity > 1)
            {
                var badge = CreateBadge(node.Simulation.Capacity.ToString(), Brushes.Orange);
                Canvas.SetLeft(badge, terminalExtensions.Left + node.Visual.Width - 18);
                Canvas.SetTop(badge, terminalExtensions.Top + topOffset + 2);
                container.Children.Add(badge);
            }

            // Name label - position based on terminal layout
            var displayName = !string.IsNullOrEmpty(node.Name) ? node.Name : node.Type ?? "Node";
            double labelTop;
            
            if (labelOnTop)
            {
                // Label above the node
                labelTop = 0;
            }
            else
            {
                // Label inside node at bottom (38 = 36 + 2px extra spacing)
                labelTop = terminalExtensions.Top + topOffset + 38;
            }
            
            var nameText = new TextBlock
            {
                Text = displayName, FontSize = 8, FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center, Foreground = Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis, Width = node.Visual.Width - 4
            };
            Canvas.SetLeft(nameText, terminalExtensions.Left + 2);
            Canvas.SetTop(nameText, labelTop);
            container.Children.Add(nameText);

            // Label (like IN/OUT) - also adjust position
            if (!string.IsNullOrEmpty(node.Label))
            {
                var extraLabelTop = labelOnTop ? labelTop + 11 : labelTop + 11;
                
                var labelText = new TextBlock
                {
                    Text = node.Label, FontSize = 9, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center, Foreground = GetNodeBrush(node),
                    Width = node.Visual.Width - 4
                };
                Canvas.SetLeft(labelText, terminalExtensions.Left + 2);
                Canvas.SetTop(labelText, extraLabelTop);
                container.Children.Add(labelText);
            }

            // Draw protruding terminals based on position settings
            DrawNodeTerminals(container, node, isSelected, terminalExtensions, topOffset);

            // Position the container (adjusted for terminal offset and label)
            Canvas.SetLeft(container, node.Visual.X - terminalExtensions.Left);
            Canvas.SetTop(container, node.Visual.Y - terminalExtensions.Top - topOffset);

            if (node.Visual.Rotation != 0)
            {
                container.RenderTransform = new RotateTransform(
                    node.Visual.Rotation, 
                    terminalExtensions.Left + node.Visual.Width / 2, 
                    terminalExtensions.Top + topOffset + node.Visual.Height / 2);
            }

            container.Tag = node.Id;
            return container;
        }

        /// <summary>
        /// Calculate how much extra space is needed on each side for terminals
        /// </summary>
        private (double Left, double Right, double Top, double Bottom) GetTerminalExtensions(NodeData node)
        {
            double left = 0, right = 0, top = 0, bottom = 0;
            var stickOut = RenderConstants.NodeTerminalStickOut;

            // Check input terminal position
            var inputPos = node.Visual.InputTerminalPosition?.ToLower() ?? "left";
            // Check output terminal position  
            var outputPos = node.Visual.OutputTerminalPosition?.ToLower() ?? "right";

            // Determine which terminals are shown based on node type
            bool hasInput = true, hasOutput = true;
            switch (node.Type?.ToLower())
            {
                case "source": hasInput = false; break;
                case "sink": hasOutput = false; break;
            }

            // Add extensions for input terminal
            if (hasInput)
            {
                switch (inputPos)
                {
                    case "left": left = Math.Max(left, stickOut); break;
                    case "right": right = Math.Max(right, stickOut); break;
                    case "top": top = Math.Max(top, stickOut); break;
                    case "bottom": bottom = Math.Max(bottom, stickOut); break;
                }
            }

            // Add extensions for output terminal
            if (hasOutput)
            {
                switch (outputPos)
                {
                    case "left": left = Math.Max(left, stickOut); break;
                    case "right": right = Math.Max(right, stickOut); break;
                    case "top": top = Math.Max(top, stickOut); break;
                    case "bottom": bottom = Math.Max(bottom, stickOut); break;
                }
            }

            return (left, right, top, bottom);
        }

        private void DrawNodeTerminals(Canvas container, NodeData node, bool isSelected,
            (double Left, double Right, double Top, double Bottom) extensions, double topOffset = 0)
        {
            // Determine which terminals to show based on node type
            bool hasInput = true, hasOutput = true;
            switch (node.Type?.ToLower())
            {
                case "source": hasInput = false; break;
                case "sink": hasOutput = false; break;
            }

            var inputPos = node.Visual.InputTerminalPosition?.ToLower() ?? "left";
            var outputPos = node.Visual.OutputTerminalPosition?.ToLower() ?? "right";

            // Draw input terminal (green)
            if (hasInput)
            {
                var (edgePoint, outerPoint) = GetTerminalPoints(node, inputPos, extensions, true, topOffset);
                DrawNodeTerminal(container, edgePoint, outerPoint, Colors.ForestGreen, $"terminal-in:{node.Id}");
            }

            // Draw output terminal (red)
            if (hasOutput)
            {
                var (edgePoint, outerPoint) = GetTerminalPoints(node, outputPos, extensions, false, topOffset);
                DrawNodeTerminal(container, edgePoint, outerPoint, Colors.Crimson, $"terminal-out:{node.Id}");
            }
        }

        /// <summary>
        /// Calculate edge and outer points for a terminal at a given position
        /// </summary>
        private (Point EdgePoint, Point OuterPoint) GetTerminalPoints(NodeData node, string position,
            (double Left, double Right, double Top, double Bottom) extensions, bool isInput, double topOffset = 0)
        {
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            // Node rectangle offset in container
            var nodeLeft = extensions.Left;
            var nodeTop = extensions.Top + topOffset;
            
            // Center points
            var centerX = nodeLeft + w / 2;
            var centerY = nodeTop + h / 2;

            Point edgePoint, outerPoint;

            // If node has explicit normalized terminal coordinates, use them
            Point? norm = isInput ? node.Visual.TerminalInNorm : node.Visual.TerminalOutNorm;
            if (norm == null)
            {
                // Try icon metadata
                var meta = IconRegistry.Get(node.Visual.Icon);
                if (meta != null)
                {
                    norm = isInput ? meta.TerminalInNorm : meta.TerminalOutNorm;
                }
            }

            if (norm != null)
            {
                // Compute point relative to node rectangle
                var px = nodeLeft + norm.Value.X * w;
                var py = nodeTop + norm.Value.Y * h;

                // EdgePoint is at the icon edge; Outer point is offset outward by stickOut along vector from center
                var dx = px - centerX;
                var dy = py - centerY;
                var len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.1) len = 1; // avoid zero
                var nx = dx / len; var ny = dy / len;
                edgePoint = new Point(px, py);
                outerPoint = new Point(px + nx * stickOut, py + ny * stickOut);
                return (edgePoint, outerPoint);
            }

            switch (position)
            {
                case "left":
                    edgePoint = new Point(nodeLeft, centerY);
                    outerPoint = new Point(0, centerY);
                    break;
                case "right":
                    edgePoint = new Point(nodeLeft + w, centerY);
                    outerPoint = new Point(nodeLeft + w + stickOut, centerY);
                    break;
                case "top":
                    edgePoint = new Point(centerX, nodeTop);
                    outerPoint = new Point(centerX, nodeTop - stickOut);
                    break;
                case "bottom":
                    edgePoint = new Point(centerX, nodeTop + h);
                    outerPoint = new Point(centerX, nodeTop + h + stickOut);
                    break;
                default:  // Default to left for input, right for output
                    if (isInput)
                    {
                        edgePoint = new Point(nodeLeft, centerY);
                        outerPoint = new Point(0, centerY);
                    }
                    else
                    {
                        edgePoint = new Point(nodeLeft + w, centerY);
                        outerPoint = new Point(nodeLeft + w + stickOut, centerY);
                    }
                    break;
            }

            return (edgePoint, outerPoint);
        }

        private void DrawNodeTerminal(Canvas container, Point edgePoint, Point outerPoint, Color color, string tag)
        {
            // Stem line
            var stem = new Line
            {
                X1 = edgePoint.X, Y1 = edgePoint.Y,
                X2 = outerPoint.X, Y2 = outerPoint.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };
            container.Children.Add(stem);

            // Terminal circle
            var terminal = new Ellipse
            {
                Width = RenderConstants.NodeTerminalRadius * 2,
                Height = RenderConstants.NodeTerminalRadius * 2,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.5,
                Tag = tag,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Canvas.SetLeft(terminal, outerPoint.X - RenderConstants.NodeTerminalRadius);
            Canvas.SetTop(terminal, outerPoint.Y - RenderConstants.NodeTerminalRadius);
            container.Children.Add(terminal);
        }

        private UIElement? CreateIcon(NodeData node)
        {
            // Prefer icon metadata if present
            var meta = IconRegistry.Get(node.Visual.Icon);
            if (meta != null)
            {
                var filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, meta.File);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(filePath);
                        // Let WPF handle GIF animation by not freezing
                        bmp.CacheOption = BitmapCacheOption.OnDemand;
                        bmp.DecodePixelWidth = meta.Width;
                        bmp.EndInit();

                        var displayWidth = Math.Min(node.Visual.Width - 8, meta.Width);
                        var displayHeight = Math.Min(node.Visual.Height - 8, meta.Height);
                        return new Image { Source = bmp, Width = displayWidth, Height = displayHeight, Stretch = Stretch.Uniform };
                    }
                    catch { }
                }
            }

            // Fallback to existing simple loader
            var img = TryLoadImage(node.Visual.Icon) ?? TryLoadImage(node.Type);
            if (img != null)
                return new Image { Source = img, Width = 28, Height = 28, Stretch = Stretch.Uniform };
            return CreatePathIcon(node);
        }

        private BitmapImage? TryLoadImage(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_iconCache.TryGetValue(name, out var cached)) return cached;

            foreach (var ext in new[] { ".png", ".gif", ".jpg", ".jpeg" })
            {
                var filePath = IOPath.Combine(_iconFolderPath, name + ext);
                if (IOFile.Exists(filePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 64;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        _iconCache[name] = bitmap;
                        return bitmap;
                    }
                    catch { }
                }
            }
            return null;
        }

        private System.Windows.Shapes.Path? CreatePathIcon(NodeData node)
        {
            var iconKey = node.Visual.Icon ?? "cnc_mill";
            var geometry = IconLibrary.GetGeometry(iconKey);
            if (geometry == null) return null;

            var brush = GetNodeBrush(node);
            var isFilled = IconLibrary.GetIsFilled(iconKey);

            return new Path
            {
                Data = geometry,
                Stroke = isFilled ? null : brush,
                Fill = isFilled ? brush : null,
                StrokeThickness = 1.5,
                Width = 28, Height = 28,
                Stretch = Stretch.Uniform
            };
        }

        private bool IsMachineType(string? type) =>
            type == "machine" || type == "assembly" || type == "robot" || type == "inspection";

        private Border CreateBadge(string text, Brush background)
        {
            return new Border
            {
                Background = background,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(3, 0, 3, 0),
                Child = new TextBlock
                {
                    Text = text, FontSize = 7, FontWeight = FontWeights.Bold, Foreground = Brushes.White
                }
            };
        }

        private Brush GetNodeBrush(NodeData node)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(node.Visual.Color)); }
            catch { return Brushes.Gray; }
        }

        public void ClearIconCache() => _iconCache.Clear();
        public string GetIconFolderPath() => _iconFolderPath;
        
        /// <summary>
        /// Get the INPUT terminal position for a node (for path connections)
        /// Uses InputTerminalPosition setting
        /// </summary>
        public static Point GetInputTerminalPosition(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            return node.Visual.InputTerminalPosition?.ToLower() switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x - stickOut, y + h / 2)  // Default left
            };
        }
        
        /// <summary>
        /// Get the OUTPUT terminal position for a node (for path connections)
        /// Uses OutputTerminalPosition setting
        /// </summary>
        public static Point GetOutputTerminalPosition(NodeData node)
        {
            var x = node.Visual.X;
            var y = node.Visual.Y;
            var w = node.Visual.Width;
            var h = node.Visual.Height;
            var stickOut = RenderConstants.NodeTerminalStickOut;
            
            return node.Visual.OutputTerminalPosition?.ToLower() switch
            {
                "left" => new Point(x - stickOut, y + h / 2),
                "right" => new Point(x + w + stickOut, y + h / 2),
                "top" => new Point(x + w / 2, y - stickOut),
                "bottom" => new Point(x + w / 2, y + h + stickOut),
                _ => new Point(x + w + stickOut, y + h / 2)  // Default right
            };
        }
    }
}
