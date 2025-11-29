using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Floating properties panel showing selected item details
    /// </summary>
    public class PropertiesPanel : FloatingPanel
    {
        private StackPanel _contentPanel = null!;
        private TextBlock _noSelectionText = null!;
        private StackPanel _nodeProperties = null!;
        private StackPanel _pathProperties = null!;
        private StackPanel _groupProperties = null!;
        
        // Node property controls
        private TextBox _nodeNameBox = null!;
        private TextBox _nodeXBox = null!;
        private TextBox _nodeYBox = null!;
        private TextBox _nodeWidthBox = null!;
        private TextBox _nodeHeightBox = null!;
        private TextBox _nodeRotationBox = null!;
        private ComboBox _nodeTypeCombo = null!;
        
        public event Action<NodeData>? NodePropertyChanged;
        public event Action<PathData>? PathPropertyChanged;
        
        public PropertiesPanel()
        {
            Title = "Properties";
            Width = 260;
            Height = 450;
            
            BuildUI();
        }
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Header
            var header = CreateHeader("Properties");
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Scrollable content
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(6)
            };
            Grid.SetRow(scroll, 1);
            
            _contentPanel = new StackPanel();
            
            // No selection message
            _noSelectionText = new TextBlock
            {
                Text = "No element selected",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E)),
                FontStyle = FontStyles.Italic,
                FontSize = 8
            };
            _contentPanel.Children.Add(_noSelectionText);
            
            // Node properties (hidden by default)
            _nodeProperties = CreateNodePropertiesPanel();
            _nodeProperties.Visibility = Visibility.Collapsed;
            _contentPanel.Children.Add(_nodeProperties);
            
            // Path properties (hidden by default)
            _pathProperties = CreatePathPropertiesPanel();
            _pathProperties.Visibility = Visibility.Collapsed;
            _contentPanel.Children.Add(_pathProperties);
            
            // Group properties (hidden by default)
            _groupProperties = CreateGroupPropertiesPanel();
            _groupProperties.Visibility = Visibility.Collapsed;
            _contentPanel.Children.Add(_groupProperties);
            
            scroll.Content = _contentPanel;
            grid.Children.Add(scroll);
            
            Content = grid;
        }
        
        private StackPanel CreateNodePropertiesPanel()
        {
            var panel = new StackPanel();
            
            AddSectionHeader(panel, "Node Properties");
            
            // Name
            AddPropertyRow(panel, "Name:", out _nodeNameBox);
            _nodeNameBox.TextChanged += (s, e) => OnNodePropertyChanged();
            
            // Type
            panel.Children.Add(new TextBlock { Text = "Type:", FontSize = 8, Margin = new Thickness(0, 4, 0, 2) });
            _nodeTypeCombo = new ComboBox { Height = 22, FontSize = 8 };
            _nodeTypeCombo.Items.Add("machine");
            _nodeTypeCombo.Items.Add("source");
            _nodeTypeCombo.Items.Add("sink");
            _nodeTypeCombo.Items.Add("buffer");
            _nodeTypeCombo.Items.Add("conveyor");
            _nodeTypeCombo.Items.Add("robot");
            _nodeTypeCombo.Items.Add("agv");
            _nodeTypeCombo.Items.Add("inspection");
            _nodeTypeCombo.Items.Add("assembly");
            _nodeTypeCombo.Items.Add("storage");
            _nodeTypeCombo.SelectionChanged += (s, e) => OnNodePropertyChanged();
            panel.Children.Add(_nodeTypeCombo);
            
            AddSectionHeader(panel, "Position");
            
            // X, Y
            var posPanel = new StackPanel { Orientation = Orientation.Horizontal };
            posPanel.Children.Add(new TextBlock { Text = "X:", Width = 20, FontSize = 8, VerticalAlignment = VerticalAlignment.Center });
            _nodeXBox = new TextBox { Width = 60, Height = 22, FontSize = 8 };
            _nodeXBox.LostFocus += (s, e) => OnNodePropertyChanged();
            posPanel.Children.Add(_nodeXBox);
            posPanel.Children.Add(new TextBlock { Text = "Y:", Width = 20, FontSize = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
            _nodeYBox = new TextBox { Width = 60, Height = 22, FontSize = 8 };
            _nodeYBox.LostFocus += (s, e) => OnNodePropertyChanged();
            posPanel.Children.Add(_nodeYBox);
            panel.Children.Add(posPanel);
            
            // Size
            var sizePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            sizePanel.Children.Add(new TextBlock { Text = "W:", Width = 20, FontSize = 8, VerticalAlignment = VerticalAlignment.Center });
            _nodeWidthBox = new TextBox { Width = 60, Height = 22, FontSize = 8 };
            _nodeWidthBox.LostFocus += (s, e) => OnNodePropertyChanged();
            sizePanel.Children.Add(_nodeWidthBox);
            sizePanel.Children.Add(new TextBlock { Text = "H:", Width = 20, FontSize = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
            _nodeHeightBox = new TextBox { Width = 60, Height = 22, FontSize = 8 };
            _nodeHeightBox.LostFocus += (s, e) => OnNodePropertyChanged();
            sizePanel.Children.Add(_nodeHeightBox);
            panel.Children.Add(sizePanel);
            
            // Rotation
            var rotPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            rotPanel.Children.Add(new TextBlock { Text = "Rotation:", Width = 60, FontSize = 8, VerticalAlignment = VerticalAlignment.Center });
            _nodeRotationBox = new TextBox { Width = 60, Height = 22, FontSize = 8 };
            _nodeRotationBox.LostFocus += (s, e) => OnNodePropertyChanged();
            rotPanel.Children.Add(_nodeRotationBox);
            rotPanel.Children.Add(new TextBlock { Text = "°", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
            panel.Children.Add(rotPanel);
            
            return panel;
        }
        
        private StackPanel CreatePathPropertiesPanel()
        {
            var panel = new StackPanel();
            AddSectionHeader(panel, "Path Properties");
            
            panel.Children.Add(new TextBlock { Text = "From:", FontSize = 8 });
            panel.Children.Add(new TextBox { Name = "PathFromBox", Height = 22, FontSize = 8, IsReadOnly = true, Background = Brushes.WhiteSmoke });
            
            panel.Children.Add(new TextBlock { Text = "To:", FontSize = 8, Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(new TextBox { Name = "PathToBox", Height = 22, FontSize = 8, IsReadOnly = true, Background = Brushes.WhiteSmoke });
            
            panel.Children.Add(new TextBlock { Text = "Type:", FontSize = 8, Margin = new Thickness(0, 4, 0, 0) });
            var pathTypeCombo = new ComboBox { Height = 22, FontSize = 8 };
            pathTypeCombo.Items.Add("forward");
            pathTypeCombo.Items.Add("reverse");
            pathTypeCombo.Items.Add("bidirectional");
            panel.Children.Add(pathTypeCombo);
            
            return panel;
        }
        
        private StackPanel CreateGroupPropertiesPanel()
        {
            var panel = new StackPanel();
            AddSectionHeader(panel, "Group Properties");
            
            panel.Children.Add(new TextBlock { Text = "Name:", FontSize = 8 });
            panel.Children.Add(new TextBox { Height = 22, FontSize = 8 });
            
            panel.Children.Add(new TextBlock { Text = "Members:", FontSize = 8, Margin = new Thickness(0, 8, 0, 4) });
            panel.Children.Add(new ListBox { Height = 100, FontSize = 8 });
            
            var cellCheck = new CheckBox { Content = "Is Cell", FontSize = 8, Margin = new Thickness(0, 8, 0, 0) };
            panel.Children.Add(cellCheck);
            
            return panel;
        }
        
        private void AddSectionHeader(StackPanel panel, string text)
        {
            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 8,
                Margin = new Thickness(0, 8, 0, 4)
            });
        }
        
        private void AddPropertyRow(StackPanel panel, string label, out TextBox textBox)
        {
            panel.Children.Add(new TextBlock { Text = label, FontSize = 8, Margin = new Thickness(0, 4, 0, 2) });
            textBox = new TextBox { Height = 22, FontSize = 8 };
            panel.Children.Add(textBox);
        }
        
        public void ShowNodeProperties(NodeData node)
        {
            _noSelectionText.Visibility = Visibility.Collapsed;
            _nodeProperties.Visibility = Visibility.Visible;
            _pathProperties.Visibility = Visibility.Collapsed;
            _groupProperties.Visibility = Visibility.Collapsed;
            
            // Populate fields
            _nodeNameBox.Text = node.Name ?? "";
            _nodeTypeCombo.SelectedItem = node.Type;
            _nodeXBox.Text = node.Visual.X.ToString("F0");
            _nodeYBox.Text = node.Visual.Y.ToString("F0");
            _nodeWidthBox.Text = node.Visual.Width.ToString("F0");
            _nodeHeightBox.Text = node.Visual.Height.ToString("F0");
            _nodeRotationBox.Text = node.Visual.Rotation.ToString("F0");
            
            _nodeNameBox.Tag = node; // Store reference
        }
        
        public void ShowPathProperties(PathData path)
        {
            _noSelectionText.Visibility = Visibility.Collapsed;
            _nodeProperties.Visibility = Visibility.Collapsed;
            _pathProperties.Visibility = Visibility.Visible;
            _groupProperties.Visibility = Visibility.Collapsed;
        }
        
        public void ShowGroupProperties(GroupData group)
        {
            _noSelectionText.Visibility = Visibility.Collapsed;
            _nodeProperties.Visibility = Visibility.Collapsed;
            _pathProperties.Visibility = Visibility.Collapsed;
            _groupProperties.Visibility = Visibility.Visible;
        }
        
        public void ClearSelection()
        {
            _noSelectionText.Visibility = Visibility.Visible;
            _nodeProperties.Visibility = Visibility.Collapsed;
            _pathProperties.Visibility = Visibility.Collapsed;
            _groupProperties.Visibility = Visibility.Collapsed;
        }
        
        private void OnNodePropertyChanged()
        {
            if (_nodeNameBox.Tag is NodeData node)
            {
                // Update node from UI
                node.Name = _nodeNameBox.Text;
                if (_nodeTypeCombo.SelectedItem is string type)
                    node.Type = type;
                if (double.TryParse(_nodeXBox.Text, out double x))
                    node.Visual.X = x;
                if (double.TryParse(_nodeYBox.Text, out double y))
                    node.Visual.Y = y;
                if (double.TryParse(_nodeWidthBox.Text, out double w))
                    node.Visual.Width = w;
                if (double.TryParse(_nodeHeightBox.Text, out double h))
                    node.Visual.Height = h;
                if (double.TryParse(_nodeRotationBox.Text, out double r))
                    node.Visual.Rotation = r;
                    
                NodePropertyChanged?.Invoke(node);
            }
        }
    }
}
