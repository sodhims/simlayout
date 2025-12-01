using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating toolbox with draggable node icons - uses same icons as canvas
    /// </summary>
    public class ToolboxPanel : FloatingPanel
    {
        private WrapPanel _iconGrid = null!;
        
        public event Action<string>? NodeTypeSelected;
        public event Action<string>? StartNodeDrag;
        
        public ToolboxPanel()
        {
            Title = "Toolbox";
            Width = 200;
            Height = 400;
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Header
            var header = CreateHeader("Toolbox - Drag to Canvas");
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Scrollable icon grid
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(6)
            };
            Grid.SetRow(scroll, 1);
            
            _iconGrid = new WrapPanel();
            
            // Add node types - these use the SAME icons that appear on canvas
            AddNodeIcon("machine", "Machine", "#3498DB");
            AddNodeIcon("source", "Source", "#27AE60");
            AddNodeIcon("sink", "Sink", "#E74C3C");
            AddNodeIcon("buffer", "Buffer", "#F5A623");
            AddNodeIcon("conveyor", "Conveyor", "#7F8C8D");
            AddNodeIcon("robot", "Robot", "#9B59B6");
            AddNodeIcon("agv", "AGV", "#34495E");
            AddNodeIcon("inspection", "Inspection", "#1ABC9C");
            AddNodeIcon("assembly", "Assembly", "#2980B9");
            AddNodeIcon("storage", "Storage", "#95A5A6");
            AddNodeIcon("crossdock", "Crossdock", "#16A085");
            AddNodeIcon("packaging", "Packaging", "#C0392B");
            
            // Separator
            _iconGrid.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8), Width = 180 });
            
            // Infrastructure (these don't use IconLibrary)
            AddInfraIcon("wall", "Wall", "#7F8C8D", "M2,10 L22,10 L22,14 L2,14 Z");
            AddInfraIcon("column", "Column", "#566573", "M6,4 L18,4 L18,20 L6,20 Z");
            AddInfraIcon("zone", "Zone", "#85C1E9", "M4,4 L20,4 L20,20 L4,20 Z M4,4 L20,20 M20,4 L4,20");
            
            scroll.Content = _iconGrid;
            grid.Children.Add(scroll);
            
            Content = grid;
        }
        
        /// <summary>
        /// Add a node icon using the same SVG path that will appear on canvas
        /// </summary>
        private void AddNodeIcon(string type, string tooltip, string colorHex)
        {
            // Get the same icon that will be used on canvas
            var iconKey = GetIconKeyForType(type);
            var geometry = IconLibrary.GetGeometry(iconKey);
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            
            var btn = CreateIconButton(type, tooltip, color);
            
            // Create SVG path icon
            var path = new Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.5,
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            stack.Children.Add(path);
            stack.Children.Add(new TextBlock
            {
                Text = type.Length > 6 ? type.Substring(0, 5) + "." : type,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 2, 0, 0)
            });
            
            btn.Content = stack;
            _iconGrid.Children.Add(btn);
        }
        
        /// <summary>
        /// Add infrastructure icon with custom path
        /// </summary>
        private void AddInfraIcon(string type, string tooltip, string colorHex, string pathData)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var btn = CreateIconButton(type, tooltip, color, type); // toolType = type
            
            var path = new Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = new SolidColorBrush(color),
                Fill = type == "column" ? new SolidColorBrush(color) : null,
                StrokeThickness = type == "wall" ? 2 : 1,
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            stack.Children.Add(path);
            stack.Children.Add(new TextBlock
            {
                Text = type,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 2, 0, 0)
            });
            
            btn.Content = stack;
            _iconGrid.Children.Add(btn);
        }
        
        /// <summary>
        /// Get the icon key that LayoutFactory uses for this node type
        /// </summary>
        private string GetIconKeyForType(string type) => type switch
        {
            "source" => "source_arrow",
            "sink" => "sink_arrow",
            "machine" => "press_hydraulic",
            "buffer" => "buffer_fifo",
            "workstation" => "workstation_manual",
            "inspection" => "inspection_visual",
            "storage" => "shelf_unit",
            "conveyor" => "conveyor_belt",
            "junction" => "transfer_diverter",
            "agv" => "agv",
            "robot" => "robot_scara",
            "assembly" => "welder_mig",
            "crossdock" => "crossover",
            "packaging" => "container",
            _ => "cnc_mill"
        };
        
        private Button CreateIconButton(string type, string tooltip, Color color, string? toolType = null)
        {
            var btn = new Button
            {
                Width = 54,
                Height = 54,
                Margin = new Thickness(3),
                ToolTip = tooltip,
                Tag = toolType ?? type,
                Cursor = Cursors.Hand,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                BorderThickness = new Thickness(1)
            };
            
            // Mouse events for drag-and-drop
            btn.PreviewMouseLeftButtonDown += (s, e) =>
            {
                StartNodeDrag?.Invoke(type);
            };
            
            btn.Click += (s, e) =>
            {
                NodeTypeSelected?.Invoke(type);
            };
            
            // Hover effect
            btn.MouseEnter += (s, e) =>
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD));
                btn.BorderBrush = new SolidColorBrush(color);
            };
            
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
            };
            
            return btn;
        }
    }
}
