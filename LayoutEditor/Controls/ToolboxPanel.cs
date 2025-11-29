using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating toolbox with draggable node icons
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
            Height = 350;
            
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
            
            // Add node types
            AddNodeIcon("machine", "⚙️", "Machine", "#3498DB");
            AddNodeIcon("source", "📥", "Source", "#27AE60");
            AddNodeIcon("sink", "📤", "Sink", "#E74C3C");
            AddNodeIcon("buffer", "📦", "Buffer", "#F39C12");
            AddNodeIcon("conveyor", "➡️", "Conveyor", "#9B59B6");
            AddNodeIcon("robot", "🤖", "Robot", "#1ABC9C");
            AddNodeIcon("agv", "🚗", "AGV", "#34495E");
            AddNodeIcon("inspection", "🔍", "Inspection", "#E67E22");
            AddNodeIcon("assembly", "🔧", "Assembly", "#2980B9");
            AddNodeIcon("storage", "🏭", "Storage", "#8E44AD");
            AddNodeIcon("crossdock", "🔀", "Cross-dock", "#16A085");
            AddNodeIcon("packaging", "📋", "Packaging", "#C0392B");
            
            // Separator
            var sep = new Separator { Margin = new Thickness(0, 8, 0, 8) };
            _iconGrid.Children.Add(sep);
            
            // Infrastructure
            AddNodeIcon("wall", "▬", "Wall", "#7F8C8D", "wall");
            AddNodeIcon("column", "⬛", "Column", "#566573", "column");
            AddNodeIcon("zone", "⬜", "Zone", "#85C1E9", "zone");
            
            scroll.Content = _iconGrid;
            grid.Children.Add(scroll);
            
            Content = grid;
        }
        
        private void AddNodeIcon(string type, string icon, string tooltip, string colorHex, string? toolType = null)
        {
            var btn = new Button
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(3),
                ToolTip = tooltip,
                Tag = toolType ?? type,
                Cursor = Cursors.Hand,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0")),
                BorderThickness = new Thickness(1)
            };
            
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var label = new TextBlock
            {
                Text = type.Length > 6 ? type.Substring(0, 5) + "." : type,
                FontSize = 7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex))
            };
            
            stack.Children.Add(iconText);
            stack.Children.Add(label);
            btn.Content = stack;
            
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
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            };
            
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.White;
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0"));
            };
            
            _iconGrid.Children.Add(btn);
        }
    }
}
