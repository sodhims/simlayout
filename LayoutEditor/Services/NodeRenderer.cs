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
            
            // Container includes space for terminals sticking out
            var container = new Canvas
            {
                Width = node.Visual.Width + RenderConstants.NodeTerminalStickOut * 2,
                Height = node.Visual.Height,
                Background = Brushes.Transparent
            };

            // Main node rectangle (offset to allow terminal space on left)
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
            
            Canvas.SetLeft(background, RenderConstants.NodeTerminalStickOut);
            container.Children.Add(background);

            // Icon
            var icon = CreateIcon(node);
            if (icon != null)
            {
                Canvas.SetLeft(icon, RenderConstants.NodeTerminalStickOut + (node.Visual.Width - 28) / 2);
                Canvas.SetTop(icon, 4);
                container.Children.Add(icon);
            }

            // Badges
            if (IsMachineType(node.Type) && node.Simulation.Servers > 1)
            {
                var badge = CreateBadge($"x{node.Simulation.Servers}", Brushes.RoyalBlue);
                Canvas.SetLeft(badge, RenderConstants.NodeTerminalStickOut + node.Visual.Width - 18);
                Canvas.SetTop(badge, 2);
                container.Children.Add(badge);
            }
            if ((node.Type == "buffer" || node.Type == "storage") && node.Simulation.Capacity > 1)
            {
                var badge = CreateBadge(node.Simulation.Capacity.ToString(), Brushes.Orange);
                Canvas.SetLeft(badge, RenderConstants.NodeTerminalStickOut + node.Visual.Width - 18);
                Canvas.SetTop(badge, 2);
                container.Children.Add(badge);
            }

            // Name label
            var displayName = !string.IsNullOrEmpty(node.Name) ? node.Name : node.Type ?? "Node";
            var nameText = new TextBlock
            {
                Text = displayName, FontSize = 8, FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center, Foreground = Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis, Width = node.Visual.Width - 4
            };
            Canvas.SetLeft(nameText, RenderConstants.NodeTerminalStickOut + 2);
            Canvas.SetTop(nameText, 36);
            container.Children.Add(nameText);

            // Label (like IN/OUT)
            if (!string.IsNullOrEmpty(node.Label))
            {
                var labelText = new TextBlock
                {
                    Text = node.Label, FontSize = 9, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center, Foreground = GetNodeBrush(node),
                    Width = node.Visual.Width - 4
                };
                Canvas.SetLeft(labelText, RenderConstants.NodeTerminalStickOut + 2);
                Canvas.SetTop(labelText, 47);
                container.Children.Add(labelText);
            }

            // Draw protruding terminals
            DrawNodeTerminals(container, node, isSelected);

            // Position the container (adjusted for terminal offset)
            Canvas.SetLeft(container, node.Visual.X - RenderConstants.NodeTerminalStickOut);
            Canvas.SetTop(container, node.Visual.Y);

            if (node.Visual.Rotation != 0)
            {
                container.RenderTransform = new RotateTransform(
                    node.Visual.Rotation, 
                    RenderConstants.NodeTerminalStickOut + node.Visual.Width / 2, 
                    node.Visual.Height / 2);
            }

            container.Tag = node.Id;
            return container;
        }

        private void DrawNodeTerminals(Canvas container, NodeData node, bool isSelected)
        {
            var nodeColor = GetNodeBrush(node);
            
            // Terminal should be at the vertical CENTER of the node rectangle
            var nodeCenterY = node.Visual.Height / 2.0;  // = 30 for standard 60px node
            
            // Determine which terminals to show based on node type
            bool hasInput = true, hasOutput = true;
            
            switch (node.Type?.ToLower())
            {
                case "source":
                    hasInput = false;  // Sources only have output
                    break;
                case "sink":
                    hasOutput = false; // Sinks only have input
                    break;
            }

            // Input terminal (left side) - green circle
            if (hasInput)
            {
                DrawNodeTerminal(container, 
                    new Point(RenderConstants.NodeTerminalStickOut, nodeCenterY),           // Edge of node
                    new Point(0, nodeCenterY),                              // Outer point
                    Colors.ForestGreen, $"terminal-in:{node.Id}");
            }

            // Output terminal (right side) - red circle
            if (hasOutput)
            {
                DrawNodeTerminal(container,
                    new Point(RenderConstants.NodeTerminalStickOut + node.Visual.Width, nodeCenterY),              // Edge of node
                    new Point(RenderConstants.NodeTerminalStickOut * 2 + node.Visual.Width, nodeCenterY),          // Outer point
                    Colors.Crimson, $"terminal-out:{node.Id}");
            }
        }

        private void DrawNodeTerminal(Canvas container, Point edgePoint, Point outerPoint, Color color, string tag)
        {
            // Stem line - thin for small nodes
            var stem = new Line
            {
                X1 = edgePoint.X, Y1 = edgePoint.Y,
                X2 = outerPoint.X, Y2 = outerPoint.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };
            container.Children.Add(stem);

            // Terminal circle - SMALL for nodes
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
            
            // Use the IsFilled property from IconLibrary
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
        /// Terminal is at node vertical center
        /// </summary>
        public static Point GetInputTerminalPosition(NodeData node)
        {
            var nodeCenterY = node.Visual.Height / 2.0;
            return new Point(node.Visual.X - RenderConstants.NodeTerminalStickOut, node.Visual.Y + nodeCenterY);
        }
        
        /// <summary>
        /// Get the OUTPUT terminal position for a node (for path connections)
        /// </summary>
        public static Point GetOutputTerminalPosition(NodeData node)
        {
            var nodeCenterY = node.Visual.Height / 2.0;
            return new Point(node.Visual.X + node.Visual.Width + RenderConstants.NodeTerminalStickOut, node.Visual.Y + nodeCenterY);
        }
    }
}
