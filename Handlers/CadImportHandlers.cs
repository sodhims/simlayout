using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private CadImportService _cadImportService = new();

        #region CAD Import Menu Handlers

        private void ImportDxf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import CAD File",
                Filter = "All CAD Files (*.dxf;*.dwg)|*.dxf;*.dwg|DXF Files (*.dxf)|*.dxf|DWG Files (*.dwg)|*.dwg|All Files (*.*)|*.*",
                DefaultExt = ".dxf"
            };

            if (dialog.ShowDialog() == true)
            {
                ShowCadImportDialog(dialog.FileName);
            }
        }

        private void ShowCadImportDialog(string filePath)
        {
            // Get available layers
            List<string> layers;
            Rect bounds;
            try
            {
                layers = _cadImportService.GetLayers(filePath);
                bounds = _cadImportService.GetBounds(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CAD file:\n{ex.Message}", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create import options dialog
            var optionsWindow = new Window
            {
                Title = "CAD Import Options",
                Width = 500,
                Height = 750,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize,
                MinHeight = 500,
                MinWidth = 400
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Scrollable content area
            var scrollViewer = new ScrollViewer 
            { 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            
            var contentStack = new StackPanel();

            // Options panel
            var optionsPanel = new StackPanel { Margin = new Thickness(15) };
            
            var fileName = System.IO.Path.GetFileName(filePath);
            var fileExt = System.IO.Path.GetExtension(filePath).ToUpper();
            optionsPanel.Children.Add(new TextBlock 
            { 
                Text = $"{fileName} ({fileExt})", 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Show CAD bounds info
            if (!bounds.IsEmpty)
            {
                optionsPanel.Children.Add(new TextBlock 
                { 
                    Text = $"CAD size: {bounds.Width:F1} × {bounds.Height:F1} units", 
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // === FIT TO CANVAS SECTION ===
            var fitPanel = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8))
            };
            
            var fitStack = new StackPanel();
            var fitCheck = new CheckBox 
            { 
                Content = "Fit to canvas (auto-calculate scale)", 
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            fitStack.Children.Add(fitCheck);

            var fitDetailsPanel = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
            
            // Use layout canvas settings as default, or fall back to actual canvas size
            // For visible viewport, user should enter the visible area dimensions
            double defaultWidth = _layout.Canvas.Width > 0 ? _layout.Canvas.Width : 
                                  (EditorCanvas.ActualWidth > 0 ? EditorCanvas.ActualWidth : 1200);
            double defaultHeight = _layout.Canvas.Height > 0 ? _layout.Canvas.Height : 
                                   (EditorCanvas.ActualHeight > 0 ? EditorCanvas.ActualHeight : 800);
            
            var canvasSizePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            canvasSizePanel.Children.Add(new TextBlock { Text = "Fit to:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var canvasWBox = new TextBox { Text = defaultWidth.ToString("F0"), Width = 60 };
            canvasSizePanel.Children.Add(canvasWBox);
            canvasSizePanel.Children.Add(new TextBlock { Text = " × ", VerticalAlignment = VerticalAlignment.Center });
            var canvasHBox = new TextBox { Text = defaultHeight.ToString("F0"), Width = 60 };
            canvasSizePanel.Children.Add(canvasHBox);
            canvasSizePanel.Children.Add(new TextBlock { Text = " px", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
            fitDetailsPanel.Children.Add(canvasSizePanel);
            
            // Tip about visible area
            fitDetailsPanel.Children.Add(new TextBlock 
            { 
                Text = "Tip: Enter your visible window size for best fit", 
                FontSize = 10, 
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(60, 0, 0, 2)
            });

            var marginPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            marginPanel.Children.Add(new TextBlock { Text = "Margin:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var marginBox = new TextBox { Text = "50", Width = 60 };
            marginPanel.Children.Add(marginBox);
            marginPanel.Children.Add(new TextBlock { Text = " px from edge", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
            fitDetailsPanel.Children.Add(marginPanel);

            // Calculated scale preview
            var calcScaleText = new TextBlock 
            { 
                Text = "", 
                Foreground = Brushes.DarkGreen,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            fitDetailsPanel.Children.Add(calcScaleText);

            fitStack.Children.Add(fitDetailsPanel);
            fitPanel.Child = fitStack;
            optionsPanel.Children.Add(fitPanel);

            // Update calculated scale when values change
            Action updateCalcScale = () =>
            {
                if (fitCheck.IsChecked == true && !bounds.IsEmpty)
                {
                    if (double.TryParse(canvasWBox.Text, out double cw) &&
                        double.TryParse(canvasHBox.Text, out double ch) &&
                        double.TryParse(marginBox.Text, out double margin))
                    {
                        var scale = _cadImportService.CalculateFitScale(bounds, cw, ch, margin);
                        calcScaleText.Text = $"Calculated scale: {scale:F2}";
                    }
                }
                else
                {
                    calcScaleText.Text = "";
                }
            };

            fitCheck.Checked += (s, ev) => { fitDetailsPanel.IsEnabled = true; updateCalcScale(); };
            fitCheck.Unchecked += (s, ev) => { fitDetailsPanel.IsEnabled = false; calcScaleText.Text = ""; };
            canvasWBox.TextChanged += (s, ev) => updateCalcScale();
            canvasHBox.TextChanged += (s, ev) => updateCalcScale();
            marginBox.TextChanged += (s, ev) => updateCalcScale();
            updateCalcScale();

            // === MANUAL SCALE SECTION ===
            var manualPanel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
            
            // Scale
            var scalePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            scalePanel.Children.Add(new TextBlock { Text = "Scale:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var scaleBox = new TextBox { Text = "1.0", Width = 80 };
            scalePanel.Children.Add(scaleBox);
            scalePanel.Children.Add(new TextBlock { Text = " (manual override)", Foreground = Brushes.Gray, Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            manualPanel.Children.Add(scalePanel);

            // Preset buttons
            var presetPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(100, 0, 0, 10) };
            presetPanel.Children.Add(new TextBlock { Text = "Presets: ", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });
            foreach (var (label, value) in new[] { ("1:1", "1"), ("10:1", "10"), ("50:1", "50"), ("100:1", "100") })
            {
                var btn = new Button { Content = label, Width = 40, Margin = new Thickness(2, 0, 2, 0), Padding = new Thickness(2) };
                var val = value;
                btn.Click += (s, ev) => { scaleBox.Text = val; fitCheck.IsChecked = false; };
                presetPanel.Children.Add(btn);
            }
            manualPanel.Children.Add(presetPanel);

            // Offset
            var offsetXPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            offsetXPanel.Children.Add(new TextBlock { Text = "Offset X:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var offsetXBox = new TextBox { Text = "100", Width = 80 };
            offsetXPanel.Children.Add(offsetXBox);
            manualPanel.Children.Add(offsetXPanel);

            var offsetYPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            offsetYPanel.Children.Add(new TextBlock { Text = "Offset Y:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var offsetYBox = new TextBox { Text = "100", Width = 80 };
            offsetYPanel.Children.Add(offsetYBox);
            manualPanel.Children.Add(offsetYPanel);

            // Link manual settings to fit checkbox
            fitCheck.Checked += (s, ev) => manualPanel.IsEnabled = false;
            fitCheck.Unchecked += (s, ev) => manualPanel.IsEnabled = true;
            manualPanel.IsEnabled = fitCheck.IsChecked != true;

            optionsPanel.Children.Add(manualPanel);

            // Flip Y checkbox
            var flipYCheck = new CheckBox 
            { 
                Content = "Flip Y axis (CAD uses bottom-left origin)", 
                IsChecked = true, 
                Margin = new Thickness(0, 5, 0, 5) 
            };
            optionsPanel.Children.Add(flipYCheck);

            // Separator
            optionsPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            // Import options
            optionsPanel.Children.Add(new TextBlock { Text = "Import as Walls:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 5) });
            var importLinesCheck = new CheckBox { Content = "Lines", IsChecked = true, Margin = new Thickness(15, 3, 0, 3) };
            var importPolylinesCheck = new CheckBox { Content = "Polylines", IsChecked = true, Margin = new Thickness(15, 3, 0, 3) };
            var importArcsCheck = new CheckBox { Content = "Arcs (as segments)", IsChecked = true, Margin = new Thickness(15, 3, 0, 3) };
            optionsPanel.Children.Add(importLinesCheck);
            optionsPanel.Children.Add(importPolylinesCheck);
            optionsPanel.Children.Add(importArcsCheck);

            optionsPanel.Children.Add(new TextBlock { Text = "Other:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var importCirclesCheck = new CheckBox { Content = "Circles as node markers", IsChecked = false, Margin = new Thickness(15, 3, 0, 3) };
            var storeLayersCheck = new CheckBox { Content = "Store CAD layer names on walls", IsChecked = true, Margin = new Thickness(15, 3, 0, 3) };
            optionsPanel.Children.Add(importCirclesCheck);
            optionsPanel.Children.Add(storeLayersCheck);

            // Wall settings
            optionsPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            
            var thicknessPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            thicknessPanel.Children.Add(new TextBlock { Text = "Wall thickness:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var thicknessBox = new TextBox { Text = "4", Width = 50 };
            thicknessPanel.Children.Add(thicknessBox);
            optionsPanel.Children.Add(thicknessPanel);

            var minLenPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            minLenPanel.Children.Add(new TextBlock { Text = "Min line length:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var minLenBox = new TextBox { Text = "5", Width = 50 };
            minLenPanel.Children.Add(minLenBox);
            optionsPanel.Children.Add(minLenPanel);

            Grid.SetRow(optionsPanel, 0);
            contentStack.Children.Add(optionsPanel);

            // Layers panel
            var layersPanel = new StackPanel { Margin = new Thickness(15, 0, 15, 10) };
            layersPanel.Children.Add(new TextBlock 
            { 
                Text = $"Layers ({layers.Count}):", 
                FontWeight = FontWeights.SemiBold, 
                Margin = new Thickness(0, 0, 0, 5) 
            });
            
            var layerList = new ListBox 
            { 
                Height = 100,
                SelectionMode = SelectionMode.Multiple
            };
            foreach (var layer in layers)
            {
                var item = new ListBoxItem { Content = layer, IsSelected = true };
                layerList.Items.Add(item);
            }
            layersPanel.Children.Add(layerList);
            
            var layerBtnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            var selectAllBtn = new Button { Content = "Select All", Width = 75, Margin = new Thickness(0, 0, 5, 0) };
            var selectNoneBtn = new Button { Content = "Select None", Width = 80 };
            selectAllBtn.Click += (s, ev) => { foreach (ListBoxItem i in layerList.Items) i.IsSelected = true; };
            selectNoneBtn.Click += (s, ev) => { foreach (ListBoxItem i in layerList.Items) i.IsSelected = false; };
            layerBtnPanel.Children.Add(selectAllBtn);
            layerBtnPanel.Children.Add(selectNoneBtn);
            layersPanel.Children.Add(layerBtnPanel);
            
            // Update fit calculation when layers change
            layerList.SelectionChanged += (s, ev) =>
            {
                var selectedLayers = layerList.Items.Cast<ListBoxItem>()
                    .Where(i => i.IsSelected)
                    .Select(i => i.Content?.ToString() ?? "")
                    .Where(str => !string.IsNullOrEmpty(str))
                    .ToList();
                
                // Recalculate bounds for selected layers only
                if (selectedLayers.Count > 0)
                {
                    bounds = _cadImportService.GetBounds(filePath, selectedLayers);
                    updateCalcScale();
                }
            };

            Grid.SetRow(layersPanel, 1);
            contentStack.Children.Add(layersPanel);
            
            // Add content to scroll viewer and grid
            scrollViewer.Content = contentStack;
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);

            // Info text
            var infoText = new TextBlock 
            { 
                Text = "Using ACadSharp library for DXF/DWG parsing",
                Margin = new Thickness(15, 5, 15, 5),
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic
            };
            Grid.SetRow(infoText, 1);
            mainGrid.Children.Add(infoText);

            // Buttons
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15)
            };
            
            var importBtn = new Button 
            { 
                Content = "Import", 
                Width = 90, 
                IsDefault = true, 
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(5)
            };
            var cancelBtn = new Button 
            { 
                Content = "Cancel", 
                Width = 90, 
                IsCancel = true,
                Padding = new Thickness(5)
            };
            
            importBtn.Click += (s, ev) =>
            {
                if (!double.TryParse(scaleBox.Text, out double scale)) scale = 1.0;
                if (!double.TryParse(offsetXBox.Text, out double offsetX)) offsetX = 100;
                if (!double.TryParse(offsetYBox.Text, out double offsetY)) offsetY = 100;
                if (!double.TryParse(minLenBox.Text, out double minLen)) minLen = 5;
                if (!double.TryParse(thicknessBox.Text, out double thickness)) thickness = 4;
                if (!double.TryParse(canvasWBox.Text, out double canvasW)) canvasW = 1200;
                if (!double.TryParse(canvasHBox.Text, out double canvasH)) canvasH = 800;
                if (!double.TryParse(marginBox.Text, out double margin)) margin = 50;

                var selectedLayers = layerList.Items.Cast<ListBoxItem>()
                    .Where(i => i.IsSelected)
                    .Select(i => i.Content?.ToString() ?? "")
                    .Where(str => !string.IsNullOrEmpty(str))
                    .ToList();

                if (selectedLayers.Count == 0)
                {
                    MessageBox.Show("Please select at least one layer.", "No Layers", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var options = new CadImportService.CadImportOptions
                {
                    Scale = scale,
                    Offset = new Point(offsetX, offsetY),
                    FlipY = flipYCheck.IsChecked == true,
                    ImportLinesAsWalls = importLinesCheck.IsChecked == true,
                    ImportPolylinesAsWalls = importPolylinesCheck.IsChecked == true,
                    ImportArcsAsWalls = importArcsCheck.IsChecked == true,
                    ImportCirclesAsNodes = importCirclesCheck.IsChecked == true,
                    StoreLayers = storeLayersCheck.IsChecked == true,
                    MinLineLength = minLen,
                    WallThickness = thickness,
                    LayersToImport = selectedLayers,
                    FitToCanvas = fitCheck.IsChecked == true,
                    CanvasWidth = canvasW,
                    CanvasHeight = canvasH,
                    FitMargin = margin
                };

                optionsWindow.Cursor = System.Windows.Input.Cursors.Wait;
                importBtn.IsEnabled = false;

                try
                {
                    SaveUndoState();
                    var result = _cadImportService.Import(filePath, _layout, options);

                    if (result.Success)
                    {
                        MarkDirty();
                        Redraw();
                        
                        var scaleUsed = options.FitToCanvas 
                            ? _cadImportService.CalculateFitScale(result.Bounds, canvasW, canvasH, margin)
                            : scale;
                        
                        var message = $"Import complete!\n\n" +
                            $"Walls imported: {result.WallsImported}\n" +
                            $"Nodes imported: {result.NodesImported}\n" +
                            $"Entities skipped: {result.EntitiesSkipped}\n\n" +
                            $"CAD bounds: {result.Bounds.Width:F1} × {result.Bounds.Height:F1}\n" +
                            $"Scale used: {scaleUsed:F2}";
                        
                        MessageBox.Show(message, "CAD Import", MessageBoxButton.OK, MessageBoxImage.Information);
                        optionsWindow.DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show($"Import failed:\n{result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    optionsWindow.Cursor = System.Windows.Input.Cursors.Arrow;
                    importBtn.IsEnabled = true;
                }
            };

            cancelBtn.Click += (s, ev) => optionsWindow.DialogResult = false;

            buttonPanel.Children.Add(importBtn);
            buttonPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            optionsWindow.Content = mainGrid;
            optionsWindow.ShowDialog();
        }

        #endregion
    }
}
