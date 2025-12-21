using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Dynamic toolbox that generates buttons from NodeTypeConfigService.
    /// Groups buttons by category with expandable sections.
    /// </summary>
    public partial class DynamicToolbox : UserControl
    {
        private readonly NodeTypeConfigService _configService;
        private readonly IconService _iconService;

        // Icon/button sizing - smaller for compact layout
        private const int IconSize = 24;
        private const int ButtonWidth = 52;
        private const int ButtonHeight = 52;

        /// <summary>
        /// Event raised when a node type button is clicked
        /// </summary>
        public event EventHandler<string>? NodeTypeSelected;

        public DynamicToolbox()
        {
            InitializeComponent();

            _configService = NodeTypeConfigService.Instance;
            _iconService = IconService.Instance;

            // Subscribe to config changes
            _configService.ConfigurationChanged += (s, e) => RefreshToolbox();

            // Initial load
            RefreshToolbox();
        }

        /// <summary>
        /// Refresh the toolbox buttons from config
        /// </summary>
        public void RefreshToolbox()
        {
            ToolboxContainer.Children.Clear();

            try
            {
                var nodeTypes = _configService.GetAllNodeTypes().ToList();
                var categories = nodeTypes
                    .Select(n => n.Category ?? "General")
                    .Distinct()
                    .OrderBy(c => GetCategoryOrder(c))
                    .ToList();

                foreach (var category in categories)
                {
                    var categoryTypes = nodeTypes
                        .Where(n => string.Equals(n.Category, category, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(n => n.DisplayName)
                        .ToList();

                    if (categoryTypes.Count == 0) continue;

                    // Category expander
                    var expander = CreateCategoryExpander(category, categoryTypes.Count);
                    
                    // Buttons wrap panel
                    var wrapPanel = new WrapPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(2, 2, 2, 4)
                    };

                    foreach (var nodeType in categoryTypes)
                    {
                        var button = CreateNodeTypeButton(nodeType);
                        wrapPanel.Children.Add(button);
                    }

                    expander.Content = wrapPanel;
                    ToolboxContainer.Children.Add(expander);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing toolbox: {ex.Message}");
            }
        }

        private Expander CreateCategoryExpander(string category, int count)
        {
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = category,
                FontWeight = FontWeights.SemiBold,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Children.Add(new TextBlock
            {
                Text = $" ({count})",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                VerticalAlignment = VerticalAlignment.Center
            });

            return new Expander
            {
                Header = header,
                IsExpanded = category == "Flow" || category == "Machining" || category == "Assembly",
                Margin = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(0)
            };
        }

        private Button CreateNodeTypeButton(NodeTypeConfig nodeType)
        {
            // Get icon - smaller size
            var icon = _iconService.GetIcon(nodeType.Icon, IconSize);

            // Parse color
            Color nodeColor;
            try
            {
                nodeColor = (Color)ColorConverter.ConvertFromString(nodeType.Color ?? "#3498DB");
            }
            catch
            {
                nodeColor = Colors.SteelBlue;
            }

            // Create button content
            var content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Icon image
            if (icon != null)
            {
                var iconImage = new Image
                {
                    Source = icon,
                    Width = IconSize,
                    Height = IconSize,
                    Margin = new Thickness(0, 2, 0, 1)
                };
                content.Children.Add(iconImage);
            }
            else
            {
                // Fallback colored square
                var fallback = new Border
                {
                    Width = IconSize,
                    Height = IconSize,
                    Background = new SolidColorBrush(nodeColor),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 2, 0, 1)
                };
                content.Children.Add(fallback);
            }

            // Display name - truncated
            var displayName = nodeType.DisplayName ?? nodeType.Key;
            if (displayName.Length > 8)
                displayName = displayName.Substring(0, 7) + ".";
                
            var nameText = new TextBlock
            {
                Text = displayName,
                FontSize = 8,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = ButtonWidth - 4,
                Foreground = Brushes.Black
            };
            content.Children.Add(nameText);

            // Create button
            var button = new Button
            {
                Content = content,
                Tag = nodeType.Key,
                ToolTip = $"{nodeType.DisplayName}\n{nodeType.Description}",
                Width = ButtonWidth,
                Height = ButtonHeight,
                Margin = new Thickness(1),
                Padding = new Thickness(2),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Hover effect
            button.MouseEnter += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD));
                button.BorderBrush = new SolidColorBrush(nodeColor);
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = Brushes.White;
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
            };

            button.Click += (s, e) =>
            {
                NodeTypeSelected?.Invoke(this, nodeType.Key);
            };

            return button;
        }

        private int GetCategoryOrder(string category)
        {
            // Define preferred category order
            return category.ToLower() switch
            {
                "flow" => 0,
                "machining" => 1,
                "forming" => 2,
                "welding" => 3,
                "robots" => 4,
                "assembly" => 5,
                "quality" => 6,
                "thermal" => 7,
                "finishing" => 8,
                "storage" => 9,
                "transport" => 10,
                "people" => 11,
                "processing" => 2,  // Legacy
                "shipping" => 10,   // Legacy
                _ => 20
            };
        }
    }
}
