using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    public partial class TransportGroupPanel : UserControl, INotifyPropertyChanged
    {
        #region Fields
        
        private readonly List<TransportGroupBox> _groupBoxes = new();
        private static int _groupCounter = 1;
        private static int _colorIndex = 0;
        
        // Pick mode - when active, clicking canvas adds to this group
        private TransportGroupBox? _pickModeGroup = null;
        
        private static readonly string[] Colors = new[]
        {
            "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", 
            "#9B59B6", "#1ABC9C", "#E67E22", "#34495E"
        };
        
        #endregion

        #region Properties

        public ObservableCollection<TransportGroup> Groups { get; } = new();
        
        /// <summary>
        /// True when in pick mode - MainWindow should call AddPickedElement when canvas is clicked
        /// </summary>
        public bool IsPickMode => _pickModeGroup != null;
        
        /// <summary>
        /// The group currently being picked for (null if not in pick mode)
        /// </summary>
        public string? PickModeGroupId => _pickModeGroup?.Group.Id;

        private bool _isLinkMode;
        public bool IsLinkMode
        {
            get => _isLinkMode;
            set { _isLinkMode = value; OnPropertyChanged(); LinkModeChanged?.Invoke(this, value); }
        }

        private bool _isAutoConnectMode;
        public bool IsAutoConnectMode
        {
            get => _isAutoConnectMode;
            set { _isAutoConnectMode = value; OnPropertyChanged(); AutoConnectModeChanged?.Invoke(this, value); }
        }

        private bool _showLegend = true;
        public bool ShowLegend
        {
            get => _showLegend;
            set { _showLegend = value; OnPropertyChanged(); ShowLegendChanged?.Invoke(this, value); }
        }

        #endregion

        #region Events

        public event EventHandler<TransportGroup>? GroupAdded;
        public event EventHandler<TransportGroup>? GroupDeleted;
        public event EventHandler<TransportGroup>? GroupEdited;
        public event EventHandler<(string groupId, string elementId)>? MemberAdded;
        public event EventHandler<(string groupId, string elementId)>? MemberRemoved;
        public event EventHandler<string>? MemberClicked;
        public event EventHandler? AddMarkerRequested;
        public event EventHandler<bool>? LinkModeChanged;
        public event EventHandler<bool>? AutoConnectModeChanged;
        public event EventHandler<bool>? ShowLegendChanged;
        public event EventHandler<bool>? PickModeChanged;
        public event EventHandler? ResetViewRequested;

        #endregion

        public TransportGroupPanel()
        {
            InitializeComponent();
        }

        #region Group Operations

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var color = Colors[_colorIndex++ % Colors.Length];
            var group = new TransportGroup
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"T{_groupCounter++:D2}",
                DisplayName = "Transport Group",
                Color = color
            };

            Groups.Add(group);
            AddGroupBox(group);
            GroupAdded?.Invoke(this, group);
        }

        private void AddGroupBox(TransportGroup group)
        {
            var box = new TransportGroupBox(group);
            box.EditRequested += GroupBox_EditRequested;
            box.DeleteRequested += GroupBox_DeleteRequested;
            box.MemberDropped += GroupBox_MemberDropped;
            box.PickModeRequested += GroupBox_PickModeRequested;
            box.MemberClicked += GroupBox_MemberClicked;
            box.MemberRemoveRequested += GroupBox_MemberRemoveRequested;
            
            _groupBoxes.Add(box);
            GroupsPanel.Children.Add(box);
        }

        private void GroupBox_EditRequested(object? sender, TransportGroup group)
        {
            try
            {
                var dialog = new TransportGroupEditDialog(group)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    var box = _groupBoxes.FirstOrDefault(b => b.Group.Id == group.Id);
                    box?.RefreshHeader();
                    GroupEdited?.Invoke(this, group);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit dialog: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GroupBox_DeleteRequested(object? sender, TransportGroup group)
        {
            // Exit pick mode if deleting the pick group
            if (_pickModeGroup?.Group.Id == group.Id)
            {
                ExitPickMode();
            }
            
            var result = MessageBox.Show(
                $"Delete group '{group.Name}'?\n\nMembers will be unassigned but not deleted.",
                "Delete Group",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var box = _groupBoxes.FirstOrDefault(b => b.Group.Id == group.Id);
                if (box != null)
                {
                    _groupBoxes.Remove(box);
                    GroupsPanel.Children.Remove(box);
                }
                Groups.Remove(group);
                GroupDeleted?.Invoke(this, group);
            }
        }

        private void GroupBox_MemberDropped(object? sender, (TransportGroup group, string elementId, string elementName) args)
        {
            MemberAdded?.Invoke(this, (args.group.Id, args.elementId));
        }
        
        private void GroupBox_PickModeRequested(object? sender, TransportGroupBox box)
        {
            if (_pickModeGroup == box)
            {
                // Toggle off
                ExitPickMode();
            }
            else
            {
                // Exit previous pick mode if any
                if (_pickModeGroup != null)
                {
                    _pickModeGroup.SetPickMode(false);
                }
                
                // Enter pick mode for this group
                _pickModeGroup = box;
                box.SetPickMode(true);
                PickModeChanged?.Invoke(this, true);
            }
        }
        
        private void GroupBox_MemberClicked(object? sender, string elementId)
        {
            MemberClicked?.Invoke(this, elementId);
        }
        
        private void GroupBox_MemberRemoveRequested(object? sender, (TransportGroup group, string elementId) args)
        {
            MemberRemoved?.Invoke(this, (args.group.Id, args.elementId));
        }

        #endregion
        
        #region Pick Mode

        /// <summary>
        /// Called by MainWindow when a canvas element is clicked while in pick mode
        /// </summary>
        public void AddPickedElement(string elementId, string elementName)
        {
            if (_pickModeGroup == null) return;
            
            _pickModeGroup.AddMember(elementId, elementName);
            MemberAdded?.Invoke(this, (_pickModeGroup.Group.Id, elementId));
        }
        
        /// <summary>
        /// Try to add an item in pick mode. Returns (handled, message).
        /// </summary>
        public (bool handled, string message) TryAddPickedItem(string elementId, string elementName)
        {
            if (!IsPickMode || _pickModeGroup == null)
                return (false, "");
            
            if (_pickModeGroup.Group.MemberIds.Contains(elementId))
                return (true, $"'{elementName}' is already in group '{_pickModeGroup.Group.Name}'");
            
            AddPickedElement(elementId, elementName);
            return (true, $"Added '{elementName}' to group '{_pickModeGroup.Group.Name}'");
        }
        
        /// <summary>
        /// Exit pick mode
        /// </summary>
        public void ExitPickMode()
        {
            if (_pickModeGroup != null)
            {
                _pickModeGroup.SetPickMode(false);
                _pickModeGroup = null;
                PickModeChanged?.Invoke(this, false);
            }
        }

        #endregion
        
        #region Public API Methods (for compatibility with existing handlers)
        
        /// <summary>
        /// Get all transport groups
        /// </summary>
        public IEnumerable<TransportGroup> GetAllGroups() => Groups;
        
        /// <summary>
        /// Add element to a specific group by ID
        /// </summary>
        public void AddToGroup(string groupId, string elementId, string elementName)
        {
            var box = _groupBoxes.FirstOrDefault(b => b.Group.Id == groupId);
            if (box != null)
            {
                box.AddMember(elementId, elementName);
                MemberAdded?.Invoke(this, (groupId, elementId));
            }
        }
        
        /// <summary>
        /// Create a new group and add element to it
        /// </summary>
        public TransportGroup CreateNewGroupAndAdd(string elementId, string elementName)
        {
            var color = Colors[_colorIndex++ % Colors.Length];
            var group = new TransportGroup
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"T{_groupCounter++:D2}",
                DisplayName = "Transport Group",
                Color = color
            };

            Groups.Add(group);
            AddGroupBox(group);
            
            var box = _groupBoxes.Last();
            box.AddMember(elementId, elementName);
            
            GroupAdded?.Invoke(this, group);
            MemberAdded?.Invoke(this, (group.Id, elementId));
            
            return group;
        }
        
        /// <summary>
        /// Remove a member from all groups
        /// </summary>
        public void RemoveMemberFromAllGroups(string elementId)
        {
            foreach (var box in _groupBoxes)
            {
                if (box.Group.MemberIds.Contains(elementId))
                {
                    box.RemoveMember(elementId);
                    MemberRemoved?.Invoke(this, (box.Group.Id, elementId));
                }
            }
        }
        
        /// <summary>
        /// Get icon for a group type
        /// </summary>
        public static string GetGroupTypeIcon(string? groupType)
        {
            return groupType switch
            {
                "agv" => "🚗",
                "conveyor" => "⟿",
                "forklift" => "🏗",
                "eot" => "🏗",
                _ => "◆"
            };
        }

        #endregion

        #region Mode Toggles

        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            AddMarkerRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LinkMode_Checked(object sender, RoutedEventArgs e) => IsLinkMode = true;
        private void LinkMode_Unchecked(object sender, RoutedEventArgs e) => IsLinkMode = false;
        private void AutoConnect_Checked(object sender, RoutedEventArgs e) => IsAutoConnectMode = true;
        private void AutoConnect_Unchecked(object sender, RoutedEventArgs e) => IsAutoConnectMode = false;
        private void ShowLegend_Checked(object sender, RoutedEventArgs e) => ShowLegend = true;
        private void ShowLegend_Unchecked(object sender, RoutedEventArgs e) => ShowLegend = false;
        
        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            ResetViewRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        public void LoadGroups(IEnumerable<TransportGroup> groups)
        {
            ExitPickMode();
            Groups.Clear();
            _groupBoxes.Clear();
            GroupsPanel.Children.Clear();
            
            foreach (var group in groups)
            {
                Groups.Add(group);
                AddGroupBox(group);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    #region TransportGroupBox Control

    public class TransportGroupBox : Border
    {
        public TransportGroup Group { get; }
        
        private readonly StackPanel _membersPanel;
        private readonly TextBlock _headerText;
        private readonly TextBlock _subHeaderText;
        private readonly StackPanel _emptyStatePanel;
        private readonly Border _headerBorder;
        private readonly Button _pickButton;
        private bool _isInPickMode = false;
        
        public event EventHandler<TransportGroup>? EditRequested;
        public event EventHandler<TransportGroup>? DeleteRequested;
        public event EventHandler<(TransportGroup group, string elementId, string elementName)>? MemberDropped;
        public event EventHandler<TransportGroupBox>? PickModeRequested;
        public event EventHandler<string>? MemberClicked;
        public event EventHandler<(TransportGroup group, string elementId)>? MemberRemoveRequested;

        public TransportGroupBox(TransportGroup group)
        {
            Group = group;
            
            // Style the box
            Width = 200;
            MinHeight = 120;
            Margin = new Thickness(8);
            CornerRadius = new CornerRadius(6);
            BorderThickness = new Thickness(2);
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(group.Color));
            Background = Brushes.White;
            AllowDrop = true;
            
            // Shadow
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 4, ShadowDepth = 1, Opacity = 0.2
            };
            
            // Main layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Header
            _headerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(group.Color)),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Padding = new Thickness(8, 6, 8, 6)
            };
            
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var headerStack = new StackPanel();
            _headerText = new TextBlock
            {
                Text = group.Name,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 13
            };
            _subHeaderText = new TextBlock
            {
                Text = group.DisplayName,
                Foreground = Brushes.White,
                FontSize = 10,
                Opacity = 0.8
            };
            headerStack.Children.Add(_headerText);
            headerStack.Children.Add(_subHeaderText);
            Grid.SetColumn(headerStack, 0);
            headerGrid.Children.Add(headerStack);
            
            // Buttons: Pick, Edit, Delete
            var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            _pickButton = new Button
            {
                Content = "⊕",
                FontSize = 14,
                Padding = new Thickness(4, 2, 4, 2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Pick from canvas (click to toggle)"
            };
            _pickButton.Click += (s, e) => 
            {
                e.Handled = true;
                PickModeRequested?.Invoke(this, this);
            };
            
            var editBtn = new Button
            {
                Content = "✎",
                FontSize = 14,
                Padding = new Thickness(4, 2, 4, 2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Edit group"
            };
            editBtn.Click += (s, e) => 
            {
                e.Handled = true;
                EditRequested?.Invoke(this, Group);
            };
            
            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 14,
                Padding = new Thickness(4, 2, 4, 2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "Delete group"
            };
            deleteBtn.Click += (s, e) => 
            {
                e.Handled = true;
                DeleteRequested?.Invoke(this, Group);
            };
            
            buttonsStack.Children.Add(_pickButton);
            buttonsStack.Children.Add(editBtn);
            buttonsStack.Children.Add(deleteBtn);
            Grid.SetColumn(buttonsStack, 1);
            headerGrid.Children.Add(buttonsStack);
            
            _headerBorder.Child = headerGrid;
            Grid.SetRow(_headerBorder, 0);
            mainGrid.Children.Add(_headerBorder);
            
            // Content area
            var contentBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                CornerRadius = new CornerRadius(0, 0, 4, 4),
                MinHeight = 70,
                Padding = new Thickness(4)
            };
            
            var contentGrid = new Grid();
            
            // Members panel
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 100
            };
            _membersPanel = new StackPanel();
            scrollViewer.Content = _membersPanel;
            contentGrid.Children.Add(scrollViewer);
            
            // Empty state
            _emptyStatePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var emptyIcon = new TextBlock
            {
                Text = "📦",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var emptyText = new TextBlock
            {
                Text = "Click ⊕ then click canvas items",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            _emptyStatePanel.Children.Add(emptyIcon);
            _emptyStatePanel.Children.Add(emptyText);
            contentGrid.Children.Add(_emptyStatePanel);
            
            contentBorder.Child = contentGrid;
            Grid.SetRow(contentBorder, 1);
            mainGrid.Children.Add(contentBorder);
            
            Child = mainGrid;
            
            // Events
            DragEnter += OnDragEnter;
            DragLeave += OnDragLeave;
            DragOver += OnDragOver;
            Drop += OnDrop;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            
            UpdateEmptyState();
        }
        
        public void SetPickMode(bool isActive)
        {
            _isInPickMode = isActive;
            if (isActive)
            {
                BorderThickness = new Thickness(4);
                _pickButton.Background = new SolidColorBrush(System.Windows.Media.Colors.Yellow);
                _pickButton.Foreground = Brushes.Black;
                _pickButton.Content = "●";
            }
            else
            {
                BorderThickness = new Thickness(2);
                _pickButton.Background = Brushes.Transparent;
                _pickButton.Foreground = Brushes.White;
                _pickButton.Content = "⊕";
            }
        }

        public void RefreshHeader()
        {
            _headerText.Text = Group.Name;
            _subHeaderText.Text = Group.DisplayName;
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Group.Color));
            BorderBrush = brush;
            _headerBorder.Background = brush;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isInPickMode)
                BorderThickness = new Thickness(3);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isInPickMode)
                BorderThickness = new Thickness(2);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = CanAcceptDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (CanAcceptDrop(e))
            {
                BorderThickness = new Thickness(4);
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (!_isInPickMode)
                BorderThickness = new Thickness(2);
            Background = Brushes.White;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!_isInPickMode)
                BorderThickness = new Thickness(2);
            Background = Brushes.White;
            
            string? elementId = null;
            string? elementName = null;

            if (e.Data.GetDataPresent("LayoutEditor.Models.NodeData"))
            {
                dynamic node = e.Data.GetData("LayoutEditor.Models.NodeData");
                if (node != null)
                {
                    elementId = node.Id?.ToString();
                    elementName = node.Name?.ToString();
                }
            }
            else if (e.Data.GetDataPresent("NodeData"))
            {
                dynamic node = e.Data.GetData("NodeData");
                if (node != null)
                {
                    elementId = node.Id?.ToString();
                    elementName = node.Name?.ToString();
                }
            }
            else if (e.Data.GetDataPresent("TransportStation"))
            {
                dynamic station = e.Data.GetData("TransportStation");
                if (station != null)
                {
                    elementId = station.Id?.ToString();
                    elementName = station.Name?.ToString();
                }
            }
            else if (e.Data.GetDataPresent(typeof(string)))
            {
                var text = e.Data.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(text))
                {
                    elementId = text;
                    elementName = text;
                }
            }

            if (!string.IsNullOrEmpty(elementId))
            {
                if (!Group.MemberIds.Contains(elementId))
                {
                    AddMember(elementId, elementName ?? elementId);
                    MemberDropped?.Invoke(this, (Group, elementId, elementName ?? elementId));
                }
            }
            
            e.Handled = true;
        }

        private bool CanAcceptDrop(DragEventArgs e)
        {
            return e.Data.GetDataPresent("LayoutEditor.Models.NodeData") ||
                   e.Data.GetDataPresent("NodeData") ||
                   e.Data.GetDataPresent("TransportStation") ||
                   e.Data.GetDataPresent(typeof(string));
        }

        public void AddMember(string id, string name)
        {
            if (Group.MemberIds.Contains(id)) return;
            
            Group.MemberIds.Add(id);
            
            var memberBorder = new Border
            {
                Margin = new Thickness(2),
                Padding = new Thickness(6, 3, 6, 3),
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                Cursor = Cursors.Hand,
                Tag = id
            };
            
            var memberGrid = new Grid();
            memberGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            memberGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var memberStack = new StackPanel { Orientation = Orientation.Horizontal };
            memberStack.Children.Add(new TextBlock
            {
                Text = "◇",
                FontSize = 11,
                Margin = new Thickness(0, 0, 4, 0)
            });
            memberStack.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 120
            });
            Grid.SetColumn(memberStack, 0);
            memberGrid.Children.Add(memberStack);
            
            // Remove button
            var removeBtn = new Button
            {
                Content = "×",
                FontSize = 10,
                Padding = new Thickness(2, 0, 2, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                Cursor = Cursors.Hand,
                ToolTip = "Remove from group",
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Click += (s, e) =>
            {
                e.Handled = true;
                RemoveMember(id);
                MemberRemoveRequested?.Invoke(this, (Group, id));
            };
            Grid.SetColumn(removeBtn, 1);
            memberGrid.Children.Add(removeBtn);
            
            memberBorder.Child = memberGrid;
            memberBorder.MouseEnter += (s, e) => 
                memberBorder.Background = new SolidColorBrush(Color.FromRgb(208, 208, 208));
            memberBorder.MouseLeave += (s, e) => 
                memberBorder.Background = new SolidColorBrush(Color.FromRgb(232, 232, 232));
            memberBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 1)
                {
                    MemberClicked?.Invoke(this, id);
                }
            };
            
            _membersPanel.Children.Add(memberBorder);
            UpdateEmptyState();
        }
        
        public void RemoveMember(string id)
        {
            Group.MemberIds.Remove(id);
            
            var toRemove = _membersPanel.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag as string == id);
            if (toRemove != null)
            {
                _membersPanel.Children.Remove(toRemove);
            }
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            _emptyStatePanel.Visibility = _membersPanel.Children.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }

    #endregion

    #region Edit Dialog

    public class TransportGroupEditDialog : Window
    {
        private readonly TransportGroup _group;
        private TextBox _nameBox = null!;
        private TextBox _displayNameBox = null!;
        private ComboBox _colorBox = null!;

        private static readonly string[] Colors = new[]
        {
            "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", 
            "#9B59B6", "#1ABC9C", "#E67E22", "#34495E",
            "#C0392B", "#2980B9", "#27AE60", "#D35400"
        };

        public TransportGroupEditDialog(TransportGroup group)
        {
            _group = group;
            Title = "Edit Transport Group";
            Width = 320;
            Height = 260;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var panel = new StackPanel { Margin = new Thickness(16) };

            panel.Children.Add(new TextBlock 
            { 
                Text = "Name (prefix):", 
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            });
            _nameBox = new TextBox 
            { 
                Text = _group.Name, 
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(6, 4, 6, 4)
            };
            panel.Children.Add(_nameBox);

            panel.Children.Add(new TextBlock 
            { 
                Text = "Display Name:", 
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            });
            _displayNameBox = new TextBox 
            { 
                Text = _group.DisplayName, 
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(6, 4, 6, 4)
            };
            panel.Children.Add(_displayNameBox);

            panel.Children.Add(new TextBlock 
            { 
                Text = "Color:", 
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            });
            _colorBox = new ComboBox { Margin = new Thickness(0, 0, 0, 20), Height = 32 };
            
            foreach (var color in Colors)
            {
                var item = new ComboBoxItem
                {
                    Content = new Border
                    {
                        Width = 100,
                        Height = 20,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                        CornerRadius = new CornerRadius(3)
                    },
                    Tag = color
                };
                if (color == _group.Color)
                    item.IsSelected = true;
                _colorBox.Items.Add(item);
            }
            if (_colorBox.SelectedItem == null && _colorBox.Items.Count > 0)
                _colorBox.SelectedIndex = 0;
                
            panel.Children.Add(_colorBox);

            var buttons = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            
            var okBtn = new Button 
            { 
                Content = "OK", 
                Width = 80, 
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0), 
                IsDefault = true 
            };
            var cancelBtn = new Button 
            { 
                Content = "Cancel", 
                Width = 80,
                Height = 30,
                IsCancel = true 
            };
            
            okBtn.Click += (s, e) =>
            {
                _group.Name = _nameBox.Text;
                _group.DisplayName = _displayNameBox.Text;
                if (_colorBox.SelectedItem is ComboBoxItem item && item.Tag is string color)
                    _group.Color = color;
                DialogResult = true;
            };
            
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            panel.Children.Add(buttons);

            Content = panel;
        }
    }

    #endregion
}
