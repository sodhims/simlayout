using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LayoutEditor.Models;
using LayoutEditor.Helpers;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating layouts panel for template layouts
    /// </summary>
    public class LayoutsPanel : FloatingPanel
    {
        private WrapPanel _layoutsGrid = null!;
        private string _templatesFolder;
        
        // public event Action<string>? LayoutSelected; // Unused
        public event Action<LayoutData>? LayoutLoaded;
        
        public LayoutsPanel()
        {
            Title = "Layouts";
            Width = 350;
            Height = 450;
            
            // Default templates folder
            _templatesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LayoutEditor", "Templates"
            );
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Header
            var header = CreateHeader("Layout Templates");
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Category tabs
            var tabs = new TabControl
            {
                FontSize = 8,
                Padding = new Thickness(0)
            };
            
            var builtInTab = new TabItem { Header = "Built-in" };
            var userTab = new TabItem { Header = "My Templates" };
            var recentTab = new TabItem { Header = "Recent" };
            
            tabs.Items.Add(builtInTab);
            tabs.Items.Add(userTab);
            tabs.Items.Add(recentTab);
            tabs.SelectionChanged += (s, e) => RefreshLayouts();
            
            Grid.SetRow(tabs, 1);
            grid.Children.Add(tabs);
            
            // Layouts grid
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(6)
            };
            Grid.SetRow(scroll, 2);
            
            _layoutsGrid = new WrapPanel();
            scroll.Content = _layoutsGrid;
            grid.Children.Add(scroll);
            
            // Toolbar
            var toolbar = CreateToolbar();
            Grid.SetRow(toolbar, 3);
            grid.Children.Add(toolbar);
            
            Content = grid;
            
            // Load initial layouts
            LoadBuiltInTemplates();
        }
        
        private Border CreateToolbar()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCE, 0xDB)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(4)
            };
            
            var panel = new WrapPanel();
            
            var saveBtn = new Button { Content = "💾 Save as Template", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            saveBtn.Click += (s, e) => SaveCurrentAsTemplate();
            panel.Children.Add(saveBtn);
            
            var importBtn = new Button { Content = "📂 Import", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            importBtn.Click += (s, e) => ImportTemplate();
            panel.Children.Add(importBtn);
            
            var refreshBtn = new Button { Content = "🔄 Refresh", Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(2), FontSize = 8 };
            refreshBtn.Click += (s, e) => RefreshLayouts();
            panel.Children.Add(refreshBtn);
            
            border.Child = panel;
            return border;
        }
        
        private void LoadBuiltInTemplates()
        {
            _layoutsGrid.Children.Clear();
            
            // Built-in templates
            AddTemplateCard("Empty Layout", "Start with a blank canvas", "empty", null);
            AddTemplateCard("Simple Line", "Source → Machine → Sink", "simple_line", CreateSimpleLinePreview());
            AddTemplateCard("Parallel Lines", "Two parallel production lines", "parallel", CreateParallelPreview());
            AddTemplateCard("U-Shape", "U-shaped production flow", "u_shape", CreateUShapePreview());
            AddTemplateCard("Cell Layout", "Work cell with multiple machines", "cell", CreateCellPreview());
            AddTemplateCard("Warehouse", "Storage with AGV transport", "warehouse", CreateWarehousePreview());
            AddTemplateCard("Assembly Line", "Multi-station assembly", "assembly", CreateAssemblyPreview());
            AddTemplateCard("Job Shop", "Flexible routing layout", "jobshop", CreateJobShopPreview());
        }
        
        private void AddTemplateCard(string name, string description, string templateId, UIElement? preview)
        {
            var card = new Border
            {
                Width = 150,
                Height = 140,
                Margin = new Thickness(4),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Tag = templateId
            };
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Preview
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Name
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Description
            
            // Preview area
            var previewBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(4, 4, 0, 0)
            };
            
            if (preview != null)
                previewBorder.Child = preview;
            else
            {
                previewBorder.Child = new TextBlock
                {
                    Text = "📋",
                    FontSize = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            
            Grid.SetRow(previewBorder, 0);
            grid.Children.Add(previewBorder);
            
            // Name
            var nameText = new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 8,
                Margin = new Thickness(6, 4, 6, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);
            
            // Description
            var descText = new TextBlock
            {
                Text = description,
                FontSize = 7,
                Foreground = Brushes.Gray,
                Margin = new Thickness(6, 2, 6, 4),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(descText, 2);
            grid.Children.Add(descText);
            
            card.Child = grid;
            
            // Hover effects
            card.MouseEnter += (s, e) =>
            {
                card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
                card.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD));
            };
            
            card.MouseLeave += (s, e) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
                card.Background = Brushes.White;
            };
            
            // Click to load
            card.MouseLeftButtonDown += (s, e) =>
            {
                LoadTemplate(templateId);
            };
            
            _layoutsGrid.Children.Add(card);
        }
        
        private Canvas CreateSimpleLinePreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            canvas.Children.Add(CreatePreviewNode(10, 30, "#27AE60")); // Source
            canvas.Children.Add(CreatePreviewArrow(30, 35, 45, 35));
            canvas.Children.Add(CreatePreviewNode(55, 30, "#3498DB")); // Machine
            canvas.Children.Add(CreatePreviewArrow(75, 35, 90, 35));
            canvas.Children.Add(CreatePreviewNode(100, 30, "#E74C3C")); // Sink
            return canvas;
        }
        
        private Canvas CreateParallelPreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            // Line 1
            canvas.Children.Add(CreatePreviewNode(10, 15, "#27AE60"));
            canvas.Children.Add(CreatePreviewArrow(30, 20, 45, 20));
            canvas.Children.Add(CreatePreviewNode(55, 15, "#3498DB"));
            canvas.Children.Add(CreatePreviewArrow(75, 20, 90, 20));
            canvas.Children.Add(CreatePreviewNode(100, 15, "#E74C3C"));
            // Line 2
            canvas.Children.Add(CreatePreviewNode(10, 45, "#27AE60"));
            canvas.Children.Add(CreatePreviewArrow(30, 50, 45, 50));
            canvas.Children.Add(CreatePreviewNode(55, 45, "#3498DB"));
            canvas.Children.Add(CreatePreviewArrow(75, 50, 90, 50));
            canvas.Children.Add(CreatePreviewNode(100, 45, "#E74C3C"));
            return canvas;
        }
        
        private Canvas CreateUShapePreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            canvas.Children.Add(CreatePreviewNode(10, 15, "#27AE60"));
            canvas.Children.Add(CreatePreviewArrow(30, 20, 55, 20));
            canvas.Children.Add(CreatePreviewNode(65, 15, "#3498DB"));
            canvas.Children.Add(CreatePreviewArrow(85, 20, 100, 20));
            canvas.Children.Add(CreatePreviewNode(110, 15, "#3498DB"));
            canvas.Children.Add(CreatePreviewArrow(115, 30, 115, 40));
            canvas.Children.Add(CreatePreviewNode(110, 45, "#3498DB"));
            canvas.Children.Add(CreatePreviewArrow(100, 50, 30, 50));
            canvas.Children.Add(CreatePreviewNode(10, 45, "#E74C3C"));
            return canvas;
        }
        
        private Canvas CreateCellPreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            // Cell boundary
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = 80,
                Height = 50,
                Stroke = Brushes.DodgerBlue,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, 30);
            Canvas.SetTop(rect, 10);
            canvas.Children.Add(rect);
            // Nodes inside
            canvas.Children.Add(CreatePreviewNode(40, 20, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(70, 20, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(55, 40, "#3498DB"));
            return canvas;
        }
        
        private Canvas CreateWarehousePreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            // Storage nodes
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    canvas.Children.Add(CreatePreviewNode(20 + i * 25, 15 + j * 25, "#8E44AD"));
                }
            }
            // AGV path
            canvas.Children.Add(CreatePreviewNode(110, 30, "#34495E"));
            return canvas;
        }
        
        private Canvas CreateAssemblyPreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            canvas.Children.Add(CreatePreviewNode(10, 30, "#27AE60"));
            for (int i = 0; i < 4; i++)
            {
                canvas.Children.Add(CreatePreviewNode(35 + i * 22, 30, "#2980B9"));
                if (i < 3)
                    canvas.Children.Add(CreatePreviewArrow(50 + i * 22, 35, 55 + i * 22, 35));
            }
            canvas.Children.Add(CreatePreviewNode(125, 30, "#E74C3C"));
            return canvas;
        }
        
        private Canvas CreateJobShopPreview()
        {
            var canvas = new Canvas { Width = 140, Height = 70 };
            // Scattered machines
            canvas.Children.Add(CreatePreviewNode(20, 15, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(60, 20, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(100, 15, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(40, 45, "#3498DB"));
            canvas.Children.Add(CreatePreviewNode(80, 50, "#3498DB"));
            // Some paths
            canvas.Children.Add(CreatePreviewArrow(35, 20, 50, 25));
            canvas.Children.Add(CreatePreviewArrow(75, 25, 90, 20));
            canvas.Children.Add(CreatePreviewArrow(55, 45, 70, 50));
            return canvas;
        }
        
        private System.Windows.Shapes.Ellipse CreatePreviewNode(double x, double y, string color)
        {
            var node = new System.Windows.Shapes.Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
            };
            Canvas.SetLeft(node, x);
            Canvas.SetTop(node, y);
            return node;
        }
        
        private System.Windows.Shapes.Line CreatePreviewArrow(double x1, double y1, double x2, double y2)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            return line;
        }
        
        private void LoadTemplate(string templateId)
        {
            LayoutData layout;
            
            switch (templateId)
            {
                case "empty":
                    layout = LayoutFactory.CreateDefault();
                    break;
                case "simple_line":
                    layout = CreateSimpleLineLayout();
                    break;
                case "parallel":
                    layout = CreateParallelLayout();
                    break;
                case "u_shape":
                    layout = CreateUShapeLayout();
                    break;
                case "cell":
                    layout = CreateCellLayout();
                    break;
                case "warehouse":
                    layout = CreateWarehouseLayout();
                    break;
                case "assembly":
                    layout = CreateAssemblyLayout();
                    break;
                case "jobshop":
                    layout = CreateJobShopLayout();
                    break;
                default:
                    layout = LayoutFactory.CreateDefault();
                    break;
            }
            
            LayoutLoaded?.Invoke(layout);
        }
        
        // Template creation methods
        private LayoutData CreateSimpleLineLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Simple Line";
            
            var source = LayoutFactory.CreateNode("source", 100, 300);
            source.Name = "Source";
            var machine = LayoutFactory.CreateNode("machine", 300, 300);
            machine.Name = "Machine 1";
            var sink = LayoutFactory.CreateNode("sink", 500, 300);
            sink.Name = "Sink";
            
            layout.Nodes.Add(source);
            layout.Nodes.Add(machine);
            layout.Nodes.Add(sink);
            
            layout.Paths.Add(new PathData { From = source.Id, To = machine.Id });
            layout.Paths.Add(new PathData { From = machine.Id, To = sink.Id });
            
            return layout;
        }
        
        private LayoutData CreateParallelLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Parallel Lines";
            
            for (int line = 0; line < 2; line++)
            {
                int y = 200 + line * 200;
                var source = LayoutFactory.CreateNode("source", 100, y);
                source.Name = $"Source {line + 1}";
                var machine = LayoutFactory.CreateNode("machine", 300, y);
                machine.Name = $"Machine {line + 1}";
                var sink = LayoutFactory.CreateNode("sink", 500, y);
                sink.Name = $"Sink {line + 1}";
                
                layout.Nodes.Add(source);
                layout.Nodes.Add(machine);
                layout.Nodes.Add(sink);
                
                layout.Paths.Add(new PathData { From = source.Id, To = machine.Id });
                layout.Paths.Add(new PathData { From = machine.Id, To = sink.Id });
            }
            
            return layout;
        }
        
        private LayoutData CreateUShapeLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "U-Shape Layout";
            
            var source = LayoutFactory.CreateNode("source", 100, 200);
            var m1 = LayoutFactory.CreateNode("machine", 250, 200);
            var m2 = LayoutFactory.CreateNode("machine", 400, 200);
            var m3 = LayoutFactory.CreateNode("machine", 400, 400);
            var m4 = LayoutFactory.CreateNode("machine", 250, 400);
            var sink = LayoutFactory.CreateNode("sink", 100, 400);
            
            source.Name = "Source";
            m1.Name = "Machine 1";
            m2.Name = "Machine 2";
            m3.Name = "Machine 3";
            m4.Name = "Machine 4";
            sink.Name = "Sink";
            
            layout.Nodes.Add(source);
            layout.Nodes.Add(m1);
            layout.Nodes.Add(m2);
            layout.Nodes.Add(m3);
            layout.Nodes.Add(m4);
            layout.Nodes.Add(sink);
            
            layout.Paths.Add(new PathData { From = source.Id, To = m1.Id });
            layout.Paths.Add(new PathData { From = m1.Id, To = m2.Id });
            layout.Paths.Add(new PathData { From = m2.Id, To = m3.Id });
            layout.Paths.Add(new PathData { From = m3.Id, To = m4.Id });
            layout.Paths.Add(new PathData { From = m4.Id, To = sink.Id });
            
            return layout;
        }
        
        private LayoutData CreateCellLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Cell Layout";
            
            var m1 = LayoutFactory.CreateNode("machine", 200, 200);
            var m2 = LayoutFactory.CreateNode("machine", 350, 200);
            var m3 = LayoutFactory.CreateNode("robot", 275, 350);
            
            m1.Name = "Machine 1";
            m2.Name = "Machine 2";
            m3.Name = "Robot";
            
            layout.Nodes.Add(m1);
            layout.Nodes.Add(m2);
            layout.Nodes.Add(m3);
            
            // Create cell
            var cell = new GroupData
            {
                Name = "Work Cell 1",
                IsCell = true
            };
            cell.Members.Add(m1.Id);
            cell.Members.Add(m2.Id);
            cell.Members.Add(m3.Id);
            layout.Groups.Add(cell);
            
            return layout;
        }
        
        private LayoutData CreateWarehouseLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Warehouse Layout";
            
            // Storage locations
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var storage = LayoutFactory.CreateNode("storage", 100 + col * 120, 100 + row * 120);
                    storage.Name = $"Storage {row * 4 + col + 1}";
                    layout.Nodes.Add(storage);
                }
            }
            
            // AGV
            var agv = LayoutFactory.CreateNode("agv", 600, 250);
            agv.Name = "AGV 1";
            layout.Nodes.Add(agv);
            
            return layout;
        }
        
        private LayoutData CreateAssemblyLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Assembly Line";
            
            var source = LayoutFactory.CreateNode("source", 50, 300);
            source.Name = "Parts In";
            layout.Nodes.Add(source);
            
            string prevId = source.Id;
            for (int i = 0; i < 5; i++)
            {
                var station = LayoutFactory.CreateNode("assembly", 150 + i * 120, 300);
                station.Name = $"Station {i + 1}";
                layout.Nodes.Add(station);
                layout.Paths.Add(new PathData { From = prevId, To = station.Id });
                prevId = station.Id;
            }
            
            var sink = LayoutFactory.CreateNode("sink", 750, 300);
            sink.Name = "Finished Goods";
            layout.Nodes.Add(sink);
            layout.Paths.Add(new PathData { From = prevId, To = sink.Id });
            
            return layout;
        }
        
        private LayoutData CreateJobShopLayout()
        {
            var layout = LayoutFactory.CreateDefault();
            layout.Metadata.Name = "Job Shop";
            
            var source = LayoutFactory.CreateNode("source", 100, 300);
            source.Name = "Jobs In";
            
            var m1 = LayoutFactory.CreateNode("machine", 250, 150);
            var m2 = LayoutFactory.CreateNode("machine", 400, 150);
            var m3 = LayoutFactory.CreateNode("machine", 250, 300);
            var m4 = LayoutFactory.CreateNode("machine", 400, 300);
            var m5 = LayoutFactory.CreateNode("machine", 250, 450);
            var m6 = LayoutFactory.CreateNode("machine", 400, 450);
            
            var sink = LayoutFactory.CreateNode("sink", 550, 300);
            sink.Name = "Jobs Out";
            
            m1.Name = "Lathe 1";
            m2.Name = "Lathe 2";
            m3.Name = "Mill 1";
            m4.Name = "Mill 2";
            m5.Name = "Grinder 1";
            m6.Name = "Grinder 2";
            
            layout.Nodes.Add(source);
            layout.Nodes.Add(m1);
            layout.Nodes.Add(m2);
            layout.Nodes.Add(m3);
            layout.Nodes.Add(m4);
            layout.Nodes.Add(m5);
            layout.Nodes.Add(m6);
            layout.Nodes.Add(sink);
            
            // Flexible routing
            layout.Paths.Add(new PathData { From = source.Id, To = m1.Id });
            layout.Paths.Add(new PathData { From = source.Id, To = m3.Id });
            layout.Paths.Add(new PathData { From = source.Id, To = m5.Id });
            layout.Paths.Add(new PathData { From = m1.Id, To = m3.Id });
            layout.Paths.Add(new PathData { From = m2.Id, To = m4.Id });
            layout.Paths.Add(new PathData { From = m3.Id, To = m5.Id });
            layout.Paths.Add(new PathData { From = m4.Id, To = m6.Id });
            layout.Paths.Add(new PathData { From = m5.Id, To = sink.Id });
            layout.Paths.Add(new PathData { From = m6.Id, To = sink.Id });
            
            return layout;
        }
        
        private void RefreshLayouts()
        {
            LoadBuiltInTemplates();
        }
        
        private void SaveCurrentAsTemplate()
        {
            MessageBox.Show("Save current layout as template - to be implemented", "Save Template", MessageBoxButton.OK);
        }
        
        private void ImportTemplate()
        {
            MessageBox.Show("Import template from file - to be implemented", "Import Template", MessageBoxButton.OK);
        }
    }
}
