using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders nodes on the canvas
    /// </summary>
    public class NodeRenderer
    {
        private readonly SelectionService _selection;

        public NodeRenderer(SelectionService selection)
        {
            _selection = selection;
        }

        public void DrawNodes(Canvas canvas, LayoutData layout, 
            Action<string, UIElement> registerElement)
        {
            foreach (var node in layout.Nodes)
            {
                var element = CreateNodeElement(node);
                canvas.Children.Add(element);
                registerElement(node.Id, element);
            }
        }

        public UIElement CreateNodeElement(NodeData node)
        {
            var isSelected = _selection.IsNodeSelected(node.Id);
            var container = new Canvas
            {
                Width = node.Visual.Width,
                Height = node.Visual.Height
            };

            // Background - thinner border
            var background = new Rectangle
            {
                Width = node.Visual.Width,
                Height = node.Visual.Height,
                Fill = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)),
                Stroke = GetNodeBrush(node),
                StrokeThickness = isSelected ? 2 : 1,
                RadiusX = 4,
                RadiusY = 4
            };

            if (isSelected)
            {
                background.Stroke = new SolidColorBrush(Colors.DodgerBlue);
                background.StrokeDashArray = new DoubleCollection { 4, 2 };
            }

            container.Children.Add(background);

            // Icon
            var iconPath = CreateIconPath(node);
            if (iconPath != null)
            {
                Canvas.SetLeft(iconPath, (node.Visual.Width - 24) / 2);
                Canvas.SetTop(iconPath, 8);
                container.Children.Add(iconPath);
            }

            // Label
            if (!string.IsNullOrEmpty(node.Label))
            {
                var label = CreateLabel(node);
                container.Children.Add(label);
            }

            Canvas.SetLeft(container, node.Visual.X);
            Canvas.SetTop(container, node.Visual.Y);

            // Apply rotation if needed
            if (node.Visual.Rotation != 0)
            {
                container.RenderTransform = new RotateTransform(node.Visual.Rotation,
                    node.Visual.Width / 2, node.Visual.Height / 2);
            }

            container.Tag = node.Id;
            return container;
        }

        private Path? CreateIconPath(NodeData node)
        {
            var iconKey = node.Visual.Icon;
            var geometry = IconLibrary.GetGeometry(iconKey);
            if (geometry == null) return null;

            var brush = GetNodeBrush(node);
            
            // Check if icon should be filled (borderless)
            bool isFilled = IconLibrary.Icons.TryGetValue(iconKey, out var iconDef) && iconDef.IsFilled;

            return new Path
            {
                Data = geometry,
                Stroke = isFilled ? null : brush,
                Fill = isFilled ? brush : null,
                StrokeThickness = 1.2,
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform
            };
        }

        private TextBlock CreateLabel(NodeData node)
        {
            var label = new TextBlock
            {
                Text = node.Label,
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = node.Visual.Width - 4
            };

            // Always inside node, below the icon
            Canvas.SetLeft(label, 2);
            Canvas.SetTop(label, 34);  // Below 24px icon + 8px top margin

            return label;
        }

        private Brush GetNodeBrush(NodeData node)
        {
            try
            {
                return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(node.Visual.Color));
            }
            catch
            {
                return Brushes.Gray;
            }
        }
    }
}
