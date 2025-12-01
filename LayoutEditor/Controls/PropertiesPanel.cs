using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    public class PropertiesPanel : FloatingPanel
    {
        private StackPanel _content = null!;
        private TextBlock _noSelection = null!;
        private StackPanel _nodePanel = null!, _pathPanel = null!, _groupPanel = null!;
        
        // Node fields
        private TextBox _idBox = null!, _nameBox = null!, _labelBox = null!;
        private ComboBox _typeCombo = null!;
        private TextBox _xBox = null!, _yBox = null!, _wBox = null!, _hBox = null!, _rotBox = null!;
        private ComboBox _iconCombo = null!, _labelPosCombo = null!;
        private TextBox _colorBox = null!;
        private Border _colorPreview = null!;
        private TextBox _capBox = null!, _srvBox = null!, _processBox = null!, _setupBox = null!;
        private StackPanel _capRow = null!, _srvRow = null!, _processRow = null!, _setupRow = null!;
        private TextBlock _connText = null!;
        
        // Path fields
        private TextBox _pathIdBox = null!, _pathColorBox = null!, _speedBox = null!, _pathCapBox = null!, _distBox = null!;
        private ComboBox _fromCombo = null!, _toCombo = null!, _pathTypeCombo = null!, _routingCombo = null!, _transCombo = null!;
        
        // Group fields
        private TextBox _grpIdBox = null!, _grpNameBox = null!;
        private CheckBox _cellCheck = null!;
        private ComboBox _inPosCombo = null!, _outPosCombo = null!;
        private TextBlock _membersText = null!;
        
        private NodeData? _node; private PathData? _path; private GroupData? _group;
        private bool _updating;
        private LayoutData? _layout;
        
        public event Action<NodeData>? NodePropertyChanged;
        public event Action<PathData>? PathPropertyChanged;
        public event Action<GroupData>? GroupPropertyChanged;
        public event Action? ApplyRequested;
        
        public PropertiesPanel() { Title = "Properties"; Width = 260; Height = 480; BuildUI(); }
        public void SetLayout(LayoutData layout) => _layout = layout;
        
        private void BuildUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            grid.Children.Add(CreateHeader("Properties")); Grid.SetRow(grid.Children[0], 0);
            
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(6) };
            Grid.SetRow(scroll, 1);
            
            _content = new StackPanel();
            _noSelection = new TextBlock { Text = "No selection\n\nRight-click element → Properties", FontSize = 10, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
            _content.Children.Add(_noSelection);
            
            _nodePanel = BuildNodePanel(); _nodePanel.Visibility = Visibility.Collapsed; _content.Children.Add(_nodePanel);
            _pathPanel = BuildPathPanel(); _pathPanel.Visibility = Visibility.Collapsed; _content.Children.Add(_pathPanel);
            _groupPanel = BuildGroupPanel(); _groupPanel.Visibility = Visibility.Collapsed; _content.Children.Add(_groupPanel);
            
            scroll.Content = _content; grid.Children.Add(scroll);
            
            var btnPanel = new Border { Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)), BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(8, 6, 8, 6) };
            var applyBtn = new Button { Content = "Apply Changes", Padding = new Thickness(16, 6, 16, 6), FontSize = 10, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Stretch };
            applyBtn.Click += (s, e) => ApplyChanges();
            btnPanel.Child = applyBtn; Grid.SetRow(btnPanel, 2); grid.Children.Add(btnPanel);
            Content = grid;
        }
        
        private StackPanel BuildNodePanel()
        {
            var p = new StackPanel();
            Sec(p, "Basic");
            Row(p, "ID:", _idBox = Tb(true)); Row(p, "Type:", _typeCombo = TypeCombo()); Row(p, "Name:", _nameBox = Tb()); Row(p, "Label:", _labelBox = Tb());
            
            Sec(p, "Position");
            Row(p, "X:", _xBox = Tb()); Row(p, "Y:", _yBox = Tb()); Row(p, "W:", _wBox = Tb()); Row(p, "H:", _hBox = Tb()); Row(p, "Rot:", _rotBox = Tb());
            
            Sec(p, "Visual");
            Row(p, "Icon:", _iconCombo = IconCombo());
            var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            colorRow.Children.Add(Lbl("Color:")); 
            _colorPreview = new Border { Width = 20, Height = 20, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 4, 0), Cursor = System.Windows.Input.Cursors.Hand };
            _colorPreview.MouseLeftButtonDown += (s, e) => ShowColorPicker();
            colorRow.Children.Add(_colorPreview);
            _colorBox = new TextBox { Width = 70, Height = 20, FontSize = 9 }; _colorBox.TextChanged += (s, e) => UpdateColorPreview();
            colorRow.Children.Add(_colorBox); p.Children.Add(colorRow);
            Row(p, "LblPos:", _labelPosCombo = PosCombo(new[] { "bottom", "top", "left", "right", "center" }));
            
            Sec(p, "Simulation");
            _capRow = LabeledRow("Capacity:", _capBox = Tb()); p.Children.Add(_capRow);
            _srvRow = LabeledRow("Servers:", _srvBox = Tb()); p.Children.Add(_srvRow);
            _processRow = LabeledRow("Process:", _processBox = Tb()); p.Children.Add(_processRow);
            _setupRow = LabeledRow("Setup:", _setupBox = Tb()); p.Children.Add(_setupRow);
            
            Sec(p, "Connections");
            _connText = new TextBlock { Text = "None", FontSize = 9, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap }; p.Children.Add(_connText);
            return p;
        }
        
        private StackPanel BuildPathPanel()
        {
            var p = new StackPanel();
            Sec(p, "Path");
            Row(p, "ID:", _pathIdBox = Tb(true)); Row(p, "From:", _fromCombo = new ComboBox { Height = 20, FontSize = 9 }); Row(p, "To:", _toCombo = new ComboBox { Height = 20, FontSize = 9 });
            Row(p, "Type:", _pathTypeCombo = PosCombo(new[] { "single", "double" })); Row(p, "Route:", _routingCombo = PosCombo(new[] { "direct", "manhattan", "corridor" })); Row(p, "Color:", _pathColorBox = Tb());
            Sec(p, "Transport");
            Row(p, "Type:", _transCombo = PosCombo(new[] { "conveyor", "agv", "manual", "crane" })); Row(p, "Speed:", _speedBox = Tb()); Row(p, "Cap:", _pathCapBox = Tb()); Row(p, "Dist:", _distBox = Tb(true));
            return p;
        }
        
        private StackPanel BuildGroupPanel()
        {
            var p = new StackPanel();
            Sec(p, "Group/Cell");
            Row(p, "ID:", _grpIdBox = Tb(true)); Row(p, "Name:", _grpNameBox = Tb());
            var cellRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            cellRow.Children.Add(Lbl("Is Cell:")); _cellCheck = new CheckBox { VerticalAlignment = VerticalAlignment.Center }; cellRow.Children.Add(_cellCheck); p.Children.Add(cellRow);
            Row(p, "In:", _inPosCombo = PosCombo(new[] { "left", "right", "top", "bottom" })); Row(p, "Out:", _outPosCombo = PosCombo(new[] { "left", "right", "top", "bottom" }));
            Sec(p, "Members"); _membersText = new TextBlock { Text = "None", FontSize = 9, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap }; p.Children.Add(_membersText);
            return p;
        }
        
        private void Sec(StackPanel p, string t) => p.Children.Add(new TextBlock { Text = t, FontWeight = FontWeights.SemiBold, FontSize = 10, Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)), Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 6, 0, 4) });
        private TextBox Tb(bool ro = false) { var t = new TextBox { Height = 20, FontSize = 9, Padding = new Thickness(2), IsReadOnly = ro }; if (ro) t.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)); return t; }
        private TextBlock Lbl(string s) => new TextBlock { Text = s, FontSize = 10, Width = 50, VerticalAlignment = VerticalAlignment.Center };
        private void Row(StackPanel p, string lbl, Control c) { var r = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) }; r.Children.Add(Lbl(lbl)); c.Margin = new Thickness(0); c.Width = double.NaN; r.Children.Add(c); p.Children.Add(r); }
        private StackPanel LabeledRow(string lbl, TextBox tb) { var r = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) }; r.Children.Add(new TextBlock { Text = lbl, FontSize = 10, Width = 60 }); tb.Width = 50; r.Children.Add(tb); return r; }
        
        private ComboBox TypeCombo()
        {
            var cb = new ComboBox { Height = 20, FontSize = 9 };
            foreach (var t in new[] { "source", "sink", "machine", "buffer", "workstation", "inspection", "storage", "conveyor", "junction", "agv_station", "robot", "assembly" })
                cb.Items.Add(new ComboBoxItem { Content = t.Replace("_", " ").ToUpper(), Tag = t });
            cb.SelectionChanged += (s, e) => { if (!_updating && _node != null) UpdateSimVis(); };
            return cb;
        }
        
        private ComboBox IconCombo()
        {
            var cb = new ComboBox { Height = 20, FontSize = 9 };
            foreach (var i in new[] { "default", "machine", "robot", "conveyor", "buffer", "storage", "source", "sink", "inspection", "workstation" })
                cb.Items.Add(new ComboBoxItem { Content = i, Tag = i });
            return cb;
        }
        
        private ComboBox PosCombo(string[] opts)
        {
            var cb = new ComboBox { Height = 20, FontSize = 9 };
            foreach (var o in opts) cb.Items.Add(new ComboBoxItem { Content = o[0].ToString().ToUpper() + o.Substring(1), Tag = o });
            return cb;
        }
        
        public void ShowNodeProperties(NodeData node)
        {
            _node = node; _path = null; _group = null; _updating = true;
            _noSelection.Visibility = Visibility.Collapsed; _nodePanel.Visibility = Visibility.Visible; _pathPanel.Visibility = Visibility.Collapsed; _groupPanel.Visibility = Visibility.Collapsed;
            
            // Show shortened ID with full ID in tooltip
            var shortId = node.Id?.Length > 8 ? node.Id.Substring(0, 8) + "..." : node.Id ?? "";
            _idBox.Text = shortId; _idBox.ToolTip = node.Id;
            _nameBox.Text = node.Name ?? ""; _labelBox.Text = node.Label ?? "";
            SelTag(_typeCombo, node.Type);
            _xBox.Text = node.Visual.X.ToString("F0"); _yBox.Text = node.Visual.Y.ToString("F0"); _wBox.Text = node.Visual.Width.ToString("F0"); _hBox.Text = node.Visual.Height.ToString("F0"); _rotBox.Text = node.Visual.Rotation.ToString("F0");
            SelTag(_iconCombo, node.Visual.Icon ?? "default"); _colorBox.Text = node.Visual.Color ?? "#4A90D9"; UpdateColorPreview(); SelTag(_labelPosCombo, node.Visual.LabelPosition ?? "bottom");
            _capBox.Text = node.Simulation.Capacity.ToString(); _srvBox.Text = node.Simulation.Servers.ToString();
            _processBox.Text = node.Simulation.ProcessTime?.Value.ToString("F1") ?? "0";
            _setupBox.Text = node.Simulation.SetupTime?.Value.ToString("F1") ?? "0";
            UpdateSimVis(); UpdateConn(node);
            _updating = false;
        }
        
        public void ShowPathProperties(PathData path)
        {
            _path = path; _node = null; _group = null; _updating = true;
            _noSelection.Visibility = Visibility.Collapsed; _nodePanel.Visibility = Visibility.Collapsed; _pathPanel.Visibility = Visibility.Visible; _groupPanel.Visibility = Visibility.Collapsed;
            
            // Show shortened ID with full ID in tooltip
            var shortId = path.Id?.Length > 8 ? path.Id.Substring(0, 8) + "..." : path.Id ?? "";
            _pathIdBox.Text = shortId; _pathIdBox.ToolTip = path.Id;
            PopulateNodeCombos(); SelContent(_fromCombo, path.From); SelContent(_toCombo, path.To);
            SelTag(_pathTypeCombo, path.PathType ?? "single"); SelTag(_routingCombo, path.RoutingMode ?? "direct"); _pathColorBox.Text = path.Visual?.Color ?? "#808080";
            SelTag(_transCombo, path.Simulation?.TransportType ?? "conveyor"); _speedBox.Text = (path.Simulation?.Speed ?? 1.0).ToString("F1"); _pathCapBox.Text = (path.Simulation?.Capacity ?? 10).ToString(); _distBox.Text = CalcDist(path).ToString("F1");
            _updating = false;
        }
        
        public void ShowGroupProperties(GroupData group)
        {
            // Debug: log members
            Helpers.DebugLogger.Log($"ShowGroupProperties: {group.Name}, Members count: {group.Members?.Count ?? 0}");
            if (group.Members?.Count > 0)
            {
                Helpers.DebugLogger.Log($"  Members: {string.Join(", ", group.Members)}");
            }
            
            _group = group; _node = null; _path = null; _updating = true;
            _noSelection.Visibility = Visibility.Collapsed; _nodePanel.Visibility = Visibility.Collapsed; _pathPanel.Visibility = Visibility.Collapsed; _groupPanel.Visibility = Visibility.Visible;
            
            // Show shortened ID with full ID in tooltip
            var shortId = group.Id?.Length > 8 ? group.Id.Substring(0, 8) + "..." : group.Id ?? "";
            _grpIdBox.Text = shortId; _grpIdBox.ToolTip = group.Id;
            _grpNameBox.Text = group.Name ?? ""; _cellCheck.IsChecked = group.IsCell;
            SelTag(_inPosCombo, group.InputTerminalPosition ?? "left"); SelTag(_outPosCombo, group.OutputTerminalPosition ?? "right");
            
            // Show member NAMES not IDs
            if (group.Members?.Count > 0 && _layout != null)
            {
                var memberNames = group.Members
                    .Select(id => _layout.Nodes.FirstOrDefault(n => n.Id == id))
                    .Where(n => n != null)
                    .Select(n => n!.Name ?? n.Type ?? "?")
                    .ToList();
                _membersText.Text = string.Join(", ", memberNames);
                Helpers.DebugLogger.Log($"  Displayed: {_membersText.Text}");
            }
            else
            {
                _membersText.Text = "None";
                Helpers.DebugLogger.Log($"  No members to display (_layout null: {_layout == null})");
            }
            _updating = false;
        }
        
        public void ClearSelection() { _node = null; _path = null; _group = null; _noSelection.Visibility = Visibility.Visible; _nodePanel.Visibility = Visibility.Collapsed; _pathPanel.Visibility = Visibility.Collapsed; _groupPanel.Visibility = Visibility.Collapsed; }
        
        private void SelTag(ComboBox cb, string? tag) { if (string.IsNullOrEmpty(tag)) { cb.SelectedIndex = 0; return; } foreach (ComboBoxItem i in cb.Items) if (i.Tag as string == tag) { cb.SelectedItem = i; return; } cb.SelectedIndex = 0; }
        private void SelContent(ComboBox cb, string? c) { if (string.IsNullOrEmpty(c)) { cb.SelectedIndex = -1; return; } foreach (var i in cb.Items) if (i is ComboBoxItem ci && ci.Content as string == c) { cb.SelectedItem = ci; return; } }
        private string? GetTag(ComboBox cb) => (cb.SelectedItem as ComboBoxItem)?.Tag as string;
        
        private void UpdateSimVis()
        {
            var t = GetTag(_typeCombo);
            _capRow.Visibility = (t == "buffer" || t == "storage") ? Visibility.Visible : Visibility.Collapsed;
            _srvRow.Visibility = (t == "machine" || t == "assembly" || t == "robot" || t == "workstation") ? Visibility.Visible : Visibility.Collapsed;
            _processRow.Visibility = (t == "machine" || t == "assembly" || t == "robot" || t == "workstation" || t == "inspection") ? Visibility.Visible : Visibility.Collapsed;
            _setupRow.Visibility = (t == "machine" || t == "assembly") ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void UpdateColorPreview() { try { _colorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_colorBox.Text.StartsWith("#") ? _colorBox.Text : "#" + _colorBox.Text)); } catch { _colorPreview.Background = Brushes.Gray; } }
        
        private void ShowColorPicker()
        {
            var dlg = new Window { Title = "Color", Width = 280, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize };
            var wrap = new WrapPanel { Margin = new Thickness(10) };
            foreach (var c in new[] { "#4A90D9", "#2ECC71", "#E74C3C", "#F39C12", "#9B59B6", "#1ABC9C", "#34495E", "#95A5A6", "#000000", "#FFFFFF" })
            {
                var btn = new Button { Width = 30, Height = 30, Margin = new Thickness(2), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)), Tag = c };
                btn.Click += (s, e) => { _colorBox.Text = c; UpdateColorPreview(); dlg.Close(); };
                wrap.Children.Add(btn);
            }
            dlg.Content = wrap; dlg.ShowDialog();
        }
        
        private void UpdateConn(NodeData node)
        {
            if (_layout == null) { _connText.Text = "None"; return; }
            var ins = _layout.Paths.Where(p => p.To == node.Id).Select(p => p.From).ToList();
            var outs = _layout.Paths.Where(p => p.From == node.Id).Select(p => p.To).ToList();
            var parts = new List<string>();
            if (ins.Count > 0) parts.Add($"In: {string.Join(", ", ins)}");
            if (outs.Count > 0) parts.Add($"Out: {string.Join(", ", outs)}");
            _connText.Text = parts.Count > 0 ? string.Join("\n", parts) : "None";
        }
        
        private void PopulateNodeCombos()
        {
            _fromCombo.Items.Clear(); _toCombo.Items.Clear();
            if (_layout == null) return;
            foreach (var n in _layout.Nodes) { var d = !string.IsNullOrEmpty(n.Name) ? n.Name : n.Id; _fromCombo.Items.Add(new ComboBoxItem { Content = d, Tag = n.Id }); _toCombo.Items.Add(new ComboBoxItem { Content = d, Tag = n.Id }); }
        }
        
        private double CalcDist(PathData path)
        {
            if (_layout == null) return 0;
            var f = _layout.Nodes.FirstOrDefault(n => n.Id == path.From); var t = _layout.Nodes.FirstOrDefault(n => n.Id == path.To);
            if (f == null || t == null) return 0;
            return Math.Sqrt(Math.Pow(t.Visual.X - f.Visual.X, 2) + Math.Pow(t.Visual.Y - f.Visual.Y, 2)) / 20.0;
        }
        
        private void ApplyChanges()
        {
            if (_node != null)
            {
                _node.Name = _nameBox.Text; _node.Label = _labelBox.Text; _node.Type = GetTag(_typeCombo) ?? "machine";
                if (double.TryParse(_xBox.Text, out var x)) _node.Visual.X = x; if (double.TryParse(_yBox.Text, out var y)) _node.Visual.Y = y;
                if (double.TryParse(_wBox.Text, out var w)) _node.Visual.Width = w; if (double.TryParse(_hBox.Text, out var h)) _node.Visual.Height = h;
                if (double.TryParse(_rotBox.Text, out var r)) _node.Visual.Rotation = r;
                _node.Visual.Icon = GetTag(_iconCombo); _node.Visual.Color = _colorBox.Text; _node.Visual.LabelPosition = GetTag(_labelPosCombo);
                if (int.TryParse(_capBox.Text, out var cap)) _node.Simulation.Capacity = cap; if (int.TryParse(_srvBox.Text, out var srv)) _node.Simulation.Servers = srv;
                if (double.TryParse(_processBox.Text, out var pt)) { _node.Simulation.ProcessTime ??= new DistributionData(); _node.Simulation.ProcessTime.Value = pt; }
                if (double.TryParse(_setupBox.Text, out var st)) { _node.Simulation.SetupTime ??= new DistributionData(); _node.Simulation.SetupTime.Value = st; }
                NodePropertyChanged?.Invoke(_node);
            }
            else if (_path != null)
            {
                _path.From = ((_fromCombo.SelectedItem as ComboBoxItem)?.Tag as string) ?? _path.From; _path.To = ((_toCombo.SelectedItem as ComboBoxItem)?.Tag as string) ?? _path.To;
                _path.PathType = GetTag(_pathTypeCombo) ?? "single"; _path.RoutingMode = GetTag(_routingCombo) ?? "direct"; if (_path.Visual != null) _path.Visual.Color = _pathColorBox.Text;
                if (_path.Simulation != null) { _path.Simulation.TransportType = GetTag(_transCombo) ?? "conveyor"; if (double.TryParse(_speedBox.Text, out var spd)) _path.Simulation.Speed = spd; if (int.TryParse(_pathCapBox.Text, out var pc)) _path.Simulation.Capacity = pc; }
                PathPropertyChanged?.Invoke(_path);
            }
            else if (_group != null)
            {
                _group.Name = _grpNameBox.Text; _group.IsCell = _cellCheck.IsChecked ?? false; _group.InputTerminalPosition = GetTag(_inPosCombo) ?? "left"; _group.OutputTerminalPosition = GetTag(_outPosCombo) ?? "right";
                GroupPropertyChanged?.Invoke(_group);
            }
            ApplyRequested?.Invoke();
        }
    }
}
