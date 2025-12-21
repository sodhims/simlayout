using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating toolbox with draggable node icons organized by category
    /// </summary>
    public class ToolboxPanel : FloatingPanel
    {
        private StackPanel _categoriesPanel = null!;
        private TextBox _searchBox = null!;
        private Dictionary<string, Expander> _categoryExpanders = new();
        private Dictionary<string, Button> _allButtons = new();
        
        public event Action<string>? NodeTypeSelected;
        public event Action<string>? StartNodeDrag;
        
        // Icon size - adjust these to make icons smaller/larger
        private const double IconButtonSize = 40;
        private const double IconSize = 20;
        private const double IconFontSize = 7;
        
        public ToolboxPanel()
        {
            Title = "Toolbox";
            Width = 220;
            Height = 500;
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            
            // Header
            var header = CreateHeader("Toolbox");
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Search box
            var searchPanel = new Border
            {
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5))
            };
            _searchBox = new TextBox
            {
                Height = 24,
                Padding = new Thickness(4, 2, 4, 2),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0))
            };
            
            // Placeholder text
            var placeholder = new TextBlock
            {
                Text = "🔍 Search...",
                Foreground = Brushes.Gray,
                IsHitTestVisible = false,
                Margin = new Thickness(6, 4, 0, 0)
            };
            
            var searchGrid = new Grid();
            searchGrid.Children.Add(_searchBox);
            searchGrid.Children.Add(placeholder);
            
            _searchBox.TextChanged += (s, e) =>
            {
                placeholder.Visibility = string.IsNullOrEmpty(_searchBox.Text) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
                FilterIcons(_searchBox.Text);
            };
            
            searchPanel.Child = searchGrid;
            Grid.SetRow(searchPanel, 1);
            grid.Children.Add(searchPanel);
            
            // Scrollable categories
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(4)
            };
            Grid.SetRow(scroll, 2);
            
            _categoriesPanel = new StackPanel();
            
            // Add categories with icons
            AddCategory("Machines", new[]
            {
                ("machine", "Machine", "#3498DB", "press_hydraulic"),
                ("cnc", "CNC", "#2980B9", "cnc_mill"),
                ("lathe", "Lathe", "#3498DB", "lathe_turning"),
                ("drill", "Drill Press", "#2980B9", "drill_press"),
                ("grinder", "Grinder", "#3498DB", "grinder_surface"),
                ("injection", "Injection", "#1ABC9C", "injection_mold"),
                ("press", "Press", "#2980B9", "press_hydraulic"),
                ("furnace", "Furnace", "#E74C3C", "furnace_induction"),
                ("oven", "Oven", "#E67E22", "oven_curing"),
            });
            
            AddCategory("Assembly & Welding", new[]
            {
                ("assembly", "Assembly", "#2980B9", "welder_mig"),
                ("robot", "Robot", "#9B59B6", "robot_scara"),
                ("welder", "Welder", "#E74C3C", "welder_spot"),
                ("workstation", "Workstation", "#3498DB", "workstation_manual"),
                ("packaging", "Packaging", "#C0392B", "container"),
            });
            
            AddCategory("Inspection & QC", new[]
            {
                ("inspection", "Inspection", "#1ABC9C", "inspection_visual"),
                ("cmm", "CMM", "#16A085", "inspection_cmm"),
                ("testing", "Testing", "#1ABC9C", "inspection_visual"),
            });
            
            AddCategory("Buffers & Storage", new[]
            {
                ("buffer", "Buffer", "#F5A623", "buffer_fifo"),
                ("storage", "Storage", "#95A5A6", "shelf_unit"),
                ("rack", "Rack", "#7F8C8D", "shelf_unit"),
                ("pallet", "Pallet", "#95A5A6", "pallet_position"),
            });
            
            AddCategory("Transport", new[]
            {
                ("conveyor", "Conveyor", "#7F8C8D", "conveyor_belt"),
                ("agv", "AGV", "#34495E", "agv"),
                ("cart", "Cart", "#566573", "cart"),
                ("forklift", "Forklift", "#7F8C8D", "forklift"),
                ("crane", "Crane", "#34495E", "crane_overhead"),
                ("crossdock", "Crossdock", "#16A085", "crossover"),
            });
            
            AddCategory("Sources & Sinks", new[]
            {
                ("source", "Source", "#27AE60", "source_arrow"),
                ("sink", "Sink", "#E74C3C", "sink_arrow"),
                ("dock", "Dock", "#2ECC71", "dock_door"),
            });
            
            AddCategory("AGV Stations", new[]
            {
                ("agv_station", "AGV Station", "#9B59B6", "agv"),
                ("agv_pickup", "Pickup", "#27AE60", "agv"),
                ("agv_dropoff", "Dropoff", "#E74C3C", "agv"),
                ("agv_charging", "Charging", "#F39C12", "agv"),
            });
            
            AddCategory("Infrastructure", new[]
            {
                ("wall", "Wall", "#7F8C8D", ""),
                ("column", "Column", "#566573", ""),
                ("zone", "Zone", "#85C1E9", ""),
                ("door", "Door", "#27AE60", ""),
            }, isInfrastructure: true);
            
            scroll.Content = _categoriesPanel;
            grid.Children.Add(scroll);
            
            Content = grid;
        }
        
        private void AddCategory(string categoryName, (string type, string tooltip, string color, string iconKey)[] items, bool isInfrastructure = false)
        {
            var expander = new Expander
            {
                Header = CreateCategoryHeader(categoryName, items.Length),
                IsExpanded = categoryName == "Machines" || categoryName == "Sources & Sinks",
                Margin = new Thickness(0, 2, 0, 2),
                BorderThickness = new Thickness(0)
            };
            
            var wrapPanel = new WrapPanel
            {
                Margin = new Thickness(4, 4, 4, 8)
            };
            
            foreach (var (type, tooltip, colorHex, iconKey) in items)
            {
                Button btn;
                if (isInfrastructure)
                {
                    btn = CreateInfraButton(type, tooltip, colorHex);
                }
                else
                {
                    btn = CreateNodeButton(type, tooltip, colorHex, string.IsNullOrEmpty(iconKey) ? type : iconKey);
                }
                
                wrapPanel.Children.Add(btn);
                _allButtons[type.ToLower()] = btn;
            }
            
            expander.Content = wrapPanel;
            _categoryExpanders[categoryName] = expander;
            _categoriesPanel.Children.Add(expander);
        }
        
        private UIElement CreateCategoryHeader(string name, int count)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = $" ({count})",
                Foreground = Brushes.Gray,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }
        
        private Button CreateNodeButton(string type, string tooltip, string colorHex, string iconKey)
        {
            var geometry = IconLibrary.GetGeometry(iconKey);
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            
            var btn = CreateBaseButton(type, tooltip, color);
            
            var path = new Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.2,
                Width = IconSize,
                Height = IconSize,
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
                Text = TruncateText(type, 6),
                FontSize = IconFontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 1, 0, 0)
            });
            
            btn.Content = stack;
            return btn;
        }
        
        private Button CreateInfraButton(string type, string tooltip, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var btn = CreateBaseButton(type, tooltip, color);
            
            string pathData = type switch
            {
                "wall" => "M2,10 L22,10 L22,14 L2,14 Z",
                "column" => "M6,4 L18,4 L18,20 L6,20 Z",
                "zone" => "M4,4 L20,4 L20,20 L4,20 Z M4,4 L20,20 M20,4 L4,20",
                "door" => "M4,4 L20,4 L20,20 L4,20 Z M16,12 L18,12",
                _ => "M4,4 L20,20"
            };
            
            var path = new Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = new SolidColorBrush(color),
                Fill = type == "column" ? new SolidColorBrush(color) : null,
                StrokeThickness = type == "wall" ? 2 : 1,
                Width = IconSize,
                Height = IconSize,
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
                FontSize = IconFontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 1, 0, 0)
            });
            
            btn.Content = stack;
            return btn;
        }
        
        private Button CreateBaseButton(string type, string tooltip, Color color)
        {
            var btn = new Button
            {
                Width = IconButtonSize,
                Height = IconButtonSize,
                Margin = new Thickness(2),
                ToolTip = tooltip,
                Tag = type,
                Cursor = Cursors.Hand,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1)
            };
            
            btn.PreviewMouseLeftButtonDown += (s, e) =>
            {
                StartNodeDrag?.Invoke(type);
            };
            
            btn.Click += (s, e) =>
            {
                NodeTypeSelected?.Invoke(type);
            };
            
            btn.MouseEnter += (s, e) =>
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD));
                btn.BorderBrush = new SolidColorBrush(color);
            };
            
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            };
            
            return btn;
        }
        
        private void FilterIcons(string searchText)
        {
            var search = searchText?.ToLower().Trim() ?? "";
            
            if (string.IsNullOrEmpty(search))
            {
                // Show all
                foreach (var btn in _allButtons.Values)
                {
                    btn.Visibility = Visibility.Visible;
                }
                foreach (var expander in _categoryExpanders.Values)
                {
                    expander.Visibility = Visibility.Visible;
                }
                return;
            }
            
            // Filter buttons
            foreach (var (type, btn) in _allButtons)
            {
                var tooltip = btn.ToolTip?.ToString()?.ToLower() ?? "";
                var matches = type.Contains(search) || tooltip.Contains(search);
                btn.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Hide empty categories
            foreach (var (name, expander) in _categoryExpanders)
            {
                var wrapPanel = expander.Content as WrapPanel;
                if (wrapPanel != null)
                {
                    var hasVisibleItems = wrapPanel.Children.OfType<Button>()
                        .Any(b => b.Visibility == Visibility.Visible);
                    expander.Visibility = hasVisibleItems ? Visibility.Visible : Visibility.Collapsed;
                    if (hasVisibleItems)
                    {
                        expander.IsExpanded = true;
                    }
                }
            }
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 1) + ".";
        }
    }
}
