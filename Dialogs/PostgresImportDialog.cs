using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Dialogs
{
    /// <summary>
    /// Dialog for loading layout from PostgreSQL database
    /// </summary>
    public class PostgresImportDialog : Window
    {
        private TextBox _hostBox = null!;
        private TextBox _portBox = null!;
        private TextBox _databaseBox = null!;
        private TextBox _usernameBox = null!;
        private PasswordBox _passwordBox = null!;
        private ListBox _layoutListBox = null!;
        private TextBlock _statusText = null!;
        private Button _loadButton = null!;
        private Button _connectButton = null!;

        private List<LayoutInfo> _availableLayouts = new();
        private PostgresImportService? _importService;

        /// <summary>
        /// The loaded layout data (null if cancelled)
        /// </summary>
        public LayoutData? LoadedLayout { get; private set; }

        /// <summary>
        /// Name of the loaded layout
        /// </summary>
        public string? LoadedLayoutName { get; private set; }

        public PostgresImportDialog()
        {
            Title = "Open from PostgreSQL Database";
            Width = 550;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            MinWidth = 450;
            MinHeight = 400;

            BuildUI();
            LoadSavedConnectionSettings();
        }

        private void BuildUI()
        {
            var mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Connection Settings
            var connGroup = new GroupBox { Header = "Database Connection", Margin = new Thickness(0, 0, 0, 10) };
            var connGrid = new Grid { Margin = new Thickness(10) };
            connGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            connGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            connGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            connGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            connGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < 4; i++)
                connGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Host & Port
            AddLabel(connGrid, "Host:", 0, 0);
            _hostBox = new TextBox { Text = "localhost", Margin = new Thickness(0, 2, 5, 2) };
            Grid.SetRow(_hostBox, 0); Grid.SetColumn(_hostBox, 1);
            connGrid.Children.Add(_hostBox);

            AddLabel(connGrid, "Port:", 0, 2);
            _portBox = new TextBox { Text = "5432", Margin = new Thickness(0, 2, 5, 2) };
            Grid.SetRow(_portBox, 0); Grid.SetColumn(_portBox, 3);
            connGrid.Children.Add(_portBox);

            _connectButton = new Button { Content = "Connect", Margin = new Thickness(5, 2, 0, 2), Padding = new Thickness(10, 2, 10, 2) };
            _connectButton.Click += Connect_Click;
            Grid.SetRow(_connectButton, 0); Grid.SetColumn(_connectButton, 4);
            connGrid.Children.Add(_connectButton);

            // Database
            AddLabel(connGrid, "Database:", 1, 0);
            _databaseBox = new TextBox { Text = "layout_db", Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetRow(_databaseBox, 1); Grid.SetColumn(_databaseBox, 1); Grid.SetColumnSpan(_databaseBox, 4);
            connGrid.Children.Add(_databaseBox);

            // Username
            AddLabel(connGrid, "Username:", 2, 0);
            _usernameBox = new TextBox { Text = "postgres", Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetRow(_usernameBox, 2); Grid.SetColumn(_usernameBox, 1); Grid.SetColumnSpan(_usernameBox, 4);
            connGrid.Children.Add(_usernameBox);

            // Password
            AddLabel(connGrid, "Password:", 3, 0);
            _passwordBox = new PasswordBox { Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetRow(_passwordBox, 3); Grid.SetColumn(_passwordBox, 1); Grid.SetColumnSpan(_passwordBox, 4);
            connGrid.Children.Add(_passwordBox);

            connGroup.Content = connGrid;
            Grid.SetRow(connGroup, 0);
            mainGrid.Children.Add(connGroup);

            // Layout List
            var layoutGroup = new GroupBox { Header = "Available Layouts", Margin = new Thickness(0, 0, 0, 10) };
            
            _layoutListBox = new ListBox { Margin = new Thickness(5) };
            _layoutListBox.SelectionChanged += LayoutListBox_SelectionChanged;
            _layoutListBox.MouseDoubleClick += LayoutListBox_DoubleClick;

            // Create item template
            var template = new DataTemplate();
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
            stackFactory.SetValue(StackPanel.MarginProperty, new Thickness(5));

            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            nameFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
            stackFactory.AppendChild(nameFactory);

            var infoFactory = new FrameworkElementFactory(typeof(TextBlock));
            infoFactory.SetBinding(TextBlock.TextProperty, new Binding { 
                Converter = new LayoutInfoConverter() 
            });
            infoFactory.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Gray);
            infoFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            stackFactory.AppendChild(infoFactory);

            template.VisualTree = stackFactory;
            _layoutListBox.ItemTemplate = template;

            layoutGroup.Content = _layoutListBox;
            Grid.SetRow(layoutGroup, 1);
            mainGrid.Children.Add(layoutGroup);

            // Status
            _statusText = new TextBlock 
            { 
                TextWrapping = TextWrapping.Wrap, 
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetRow(_statusText, 2);
            mainGrid.Children.Add(_statusText);

            // Buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            
            _loadButton = new Button { Content = "Load Layout", Width = 100, Margin = new Thickness(0, 0, 10, 0), IsEnabled = false };
            _loadButton.Click += Load_Click;
            buttonPanel.Children.Add(_loadButton);

            var cancelBtn = new Button { Content = "Cancel", Width = 80, IsCancel = true };
            cancelBtn.Click += (s, e) => DialogResult = false;
            buttonPanel.Children.Add(cancelBtn);

            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void AddLabel(Grid grid, string text, int row, int col)
        {
            var label = new TextBlock 
            { 
                Text = text, 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 2, 5, 2) 
            };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, col);
            grid.Children.Add(label);
        }

        private void LoadSavedConnectionSettings()
        {
            // Could load from app settings/registry here
            // For now, use defaults
        }

        private string GetConnectionString()
        {
            return $"Host={_hostBox.Text};Port={_portBox.Text};Database={_databaseBox.Text};Username={_usernameBox.Text};Password={_passwordBox.Password}";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _statusText.Text = "Connecting...";
                _connectButton.IsEnabled = false;
                _layoutListBox.ItemsSource = null;

                _importService = new PostgresImportService(GetConnectionString());
                _availableLayouts = await _importService.GetAvailableLayoutsAsync();

                _layoutListBox.ItemsSource = _availableLayouts;

                if (_availableLayouts.Count == 0)
                {
                    _statusText.Text = "Connected. No layouts found in database.";
                }
                else
                {
                    _statusText.Text = $"Connected. Found {_availableLayouts.Count} layout(s).";
                }
            }
            catch (Exception ex)
            {
                _statusText.Text = $"Connection failed: {ex.Message}";
                MessageBox.Show($"Failed to connect to database:\n\n{ex.Message}", "Connection Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _connectButton.IsEnabled = true;
            }
        }

        private void LayoutListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _loadButton.IsEnabled = _layoutListBox.SelectedItem != null;
        }

        private void LayoutListBox_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_layoutListBox.SelectedItem != null)
            {
                Load_Click(sender, e);
            }
        }

        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            if (_layoutListBox.SelectedItem is not LayoutInfo selectedLayout)
                return;

            if (_importService == null)
            {
                MessageBox.Show("Please connect to database first.", "Not Connected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _statusText.Text = $"Loading {selectedLayout.Name}...";
                _loadButton.IsEnabled = false;

                LoadedLayout = await _importService.LoadLayoutAsync(selectedLayout.Id);
                LoadedLayoutName = selectedLayout.Name;

                _statusText.Text = "Layout loaded successfully!";
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _statusText.Text = $"Load failed: {ex.Message}";
                MessageBox.Show($"Failed to load layout:\n\n{ex.Message}", "Load Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _loadButton.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Converter for displaying layout info in the list
    /// </summary>
    public class LayoutInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LayoutInfo info)
            {
                return $"{info.NodeCount} nodes, {info.PathCount} paths, {info.CellCount} cells • Updated: {info.UpdatedAt:g}";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
