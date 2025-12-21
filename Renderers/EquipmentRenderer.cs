using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODir = System.IO.Directory;

namespace LayoutEditor.Renderers
{
    /// <summary>
    /// Renders Equipment layer (Layer 2): nodes/machines with optional clearance zones
    /// </summary>
    public class EquipmentRenderer : ILayerRenderer
    {
        private readonly SelectionService _selection;
        private readonly Dictionary<string, BitmapImage> _iconCache = new();
        private readonly string _iconFolderPath;

        // Clearance visualization toggles
        public bool ShowOperationalClearance { get; set; } = false;
        public bool ShowMaintenanceClearance { get; set; } = false;

        public EquipmentRenderer(SelectionService selection)
        {
            _selection = selection;
            _iconFolderPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeIcons");
            if (!IODir.Exists(_iconFolderPath))
                try { IODir.CreateDirectory(_iconFolderPath); } catch { }
        }

        public LayerType Layer => LayerType.Equipment;

        public int ZOrderBase => 200; // 200-299 range for Equipment

        public void Render(Canvas canvas, LayoutData layout, Action<string, UIElement> registerElement)
        {
            foreach (var node in layout.Nodes)
            {
                // Render clearance zones first (behind equipment)
                if (ShowMaintenanceClearance)
                    RenderMaintenanceClearance(canvas, node);

                if (ShowOperationalClearance)
                    RenderOperationalClearance(canvas, node);

                // Render the equipment itself
                var element = CreateNodeElement(node);
                canvas.Children.Add(element);
                registerElement(node.Id, element);
                Panel.SetZIndex(element, ZOrderBase + 50); // Equipment on top of clearances
            }
        }

        #region Clearance Rendering

        private void RenderOperationalClearance(Canvas canvas, NodeData node)
        {
            var rect = new Rectangle
            {
                Width = node.Visual.Width +
                       node.Visual.OperationalClearanceLeft +
                       node.Visual.OperationalClearanceRight,
                Height = node.Visual.Height +
                        node.Visual.OperationalClearanceTop +
                        node.Visual.OperationalClearanceBottom,
                Fill = new SolidColorBrush(Color.FromArgb(20, 70, 130, 180)), // Blue tint
                Stroke = new SolidColorBrush(Color.FromArgb(100, 70, 130, 180)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            Canvas.SetLeft(rect, node.Visual.X - node.Visual.OperationalClearanceLeft);
            Canvas.SetTop(rect, node.Visual.Y - node.Visual.OperationalClearanceTop);
            Panel.SetZIndex(rect, ZOrderBase + 10);
            canvas.Children.Add(rect);
        }

        private void RenderMaintenanceClearance(Canvas canvas, NodeData node)
        {
            var rect = new Rectangle
            {
                Width = node.Visual.Width +
                       node.Visual.MaintenanceClearanceLeft +
                       node.Visual.MaintenanceClearanceRight,
                Height = node.Visual.Height +
                        node.Visual.MaintenanceClearanceTop +
                        node.Visual.MaintenanceClearanceBottom,
                Fill = new SolidColorBrush(Color.FromArgb(15, 255, 140, 0)), // Orange tint
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 140, 0)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 6, 3 }
            };

            Canvas.SetLeft(rect, node.Visual.X - node.Visual.MaintenanceClearanceLeft);
            Canvas.SetTop(rect, node.Visual.Y - node.Visual.MaintenanceClearanceTop);
            Panel.SetZIndex(rect, ZOrderBase);
            canvas.Children.Add(rect);
        }

        #endregion

        #region Node Rendering (extracted from NodeRenderer)

        public UIElement CreateNodeElement(NodeData node)
        {
            var isSelected = _selection.IsNodeSelected(node.Id);
            var settings = EditorSettings.Instance;

            var terminalExtensions = GetTerminalExtensions(node);

            double labelExtension = 0;
            bool labelOnTop = node.Visual.OutputTerminalPosition?.ToLower() == "bottom" ||
                              node.Visual.InputTerminalPosition?.ToLower() == "bottom";
            if (labelOnTop)
                labelExtension = 14;

            var container = new Canvas
            {
                Width = node.Visual.Width + terminalExtensions.Left + terminalExtensions.Right,
                Height = node.Visual.Height + terminalExtensions.Top + terminalExtensions.Bottom + labelExtension,
                Background = Brushes.Transparent
            };

            double topOffset = labelOnTop ? labelExtension : 0;

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

            var icon = CreateIcon(node);
            if (icon != null)
            {
                Canvas.SetLeft(icon, terminalExtensions.Left + (node.Visual.Width - 28) / 2);
                Canvas.SetTop(icon, terminalExtensions.Top + topOffset + 4);
                container.Children.Add(icon);
            }

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

            var displayName = !string.IsNullOrEmpty(node.Name) ? node.Name : node.Type ?? "Node";
            double labelTop = labelOnTop ? 0 : terminalExtensions.Top + topOffset + 38;

            var nameText = new TextBlock
            {
                Text = displayName, FontSize = 8, FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center, Foreground = Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis, Width = node.Visual.Width - 4
            };
            Canvas.SetLeft(nameText, terminalExtensions.Left + 2);
            Canvas.SetTop(nameText, labelTop);
            container.Children.Add(nameText);

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

            RenderTerminals(container, node, terminalExtensions, topOffset);

            Canvas.SetLeft(container, node.Visual.X - terminalExtensions.Left);
            Canvas.SetTop(container, node.Visual.Y - terminalExtensions.Top - labelExtension);

            return container;
        }

        private (double Left, double Right, double Top, double Bottom) GetTerminalExtensions(NodeData node)
        {
            const double terminalStickout = 8;
            double left = 0, right = 0, top = 0, bottom = 0;

            if (node.Visual.InputTerminalPosition?.ToLower() == "left") left = terminalStickout;
            if (node.Visual.OutputTerminalPosition?.ToLower() == "left") left = terminalStickout;
            if (node.Visual.InputTerminalPosition?.ToLower() == "right") right = terminalStickout;
            if (node.Visual.OutputTerminalPosition?.ToLower() == "right") right = terminalStickout;
            if (node.Visual.InputTerminalPosition?.ToLower() == "top") top = terminalStickout;
            if (node.Visual.OutputTerminalPosition?.ToLower() == "top") top = terminalStickout;
            if (node.Visual.InputTerminalPosition?.ToLower() == "bottom") bottom = terminalStickout;
            if (node.Visual.OutputTerminalPosition?.ToLower() == "bottom") bottom = terminalStickout;

            return (left, right, top, bottom);
        }

        private void RenderTerminals(Canvas container, NodeData node,
            (double Left, double Right, double Top, double Bottom) extensions, double topOffset)
        {
            const double terminalRadius = 4;

            if (TerminalHelper.HasInputTerminal(node.Type))
            {
                var inputPos = TerminalHelper.GetNodeInputTerminal(node);
                var inputCircle = new Ellipse
                {
                    Width = terminalRadius * 2, Height = terminalRadius * 2,
                    Fill = Brushes.Green, Stroke = Brushes.DarkGreen, StrokeThickness = 1
                };
                Canvas.SetLeft(inputCircle, inputPos.X - node.Visual.X + extensions.Left - terminalRadius);
                Canvas.SetTop(inputCircle, inputPos.Y - node.Visual.Y + extensions.Top + topOffset - terminalRadius);
                container.Children.Add(inputCircle);
            }

            if (TerminalHelper.HasOutputTerminal(node.Type))
            {
                var outputPos = TerminalHelper.GetNodeOutputTerminal(node);
                var outputCircle = new Ellipse
                {
                    Width = terminalRadius * 2, Height = terminalRadius * 2,
                    Fill = Brushes.Red, Stroke = Brushes.DarkRed, StrokeThickness = 1
                };
                Canvas.SetLeft(outputCircle, outputPos.X - node.Visual.X + extensions.Left - terminalRadius);
                Canvas.SetTop(outputCircle, outputPos.Y - node.Visual.Y + extensions.Top + topOffset - terminalRadius);
                container.Children.Add(outputCircle);
            }
        }

        private UIElement? CreateIcon(NodeData node)
        {
            var iconName = node.Visual.Icon;
            if (string.IsNullOrEmpty(iconName)) return null;

            var iconPath = IOPath.Combine(_iconFolderPath, $"{iconName}.png");
            if (!IOFile.Exists(iconPath)) return null;

            try
            {
                if (!_iconCache.TryGetValue(iconPath, out var bitmap))
                {
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    _iconCache[iconPath] = bitmap;
                }

                return new Image { Source = bitmap, Width = 28, Height = 28 };
            }
            catch { return null; }
        }

        private UIElement CreateBadge(string text, Brush color)
        {
            var badge = new Border
            {
                Background = color,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold
                }
            };
            return badge;
        }

        private Brush GetNodeBrush(NodeData node)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(node.Visual.Color);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.SteelBlue);
            }
        }

        private bool IsMachineType(string type) =>
            type == NodeTypes.Machine || type == NodeTypes.Workstation || type == NodeTypes.Inspection;

        #endregion
    }
}
