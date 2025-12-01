using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public class EditorSettingsDialog : Window
    {
        private EditorSettings _settings;
        private CheckBox _showToolbox, _showExplorer, _showProperties, _showLayers, _showTemplates;
        private Slider _fontSlider, _paddingSlider, _lineSlider, _pathSlider;
        private TextBlock _fontLabel, _paddingLabel, _lineLabel, _pathLabel;

        public EditorSettingsDialog(EditorSettings settings)
        {
            _settings = settings;
            Title = "Editor Settings";
            Width = 320;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            BuildUI();
        }

        private void BuildUI()
        {
            var main = new StackPanel { Margin = new Thickness(12) };

            // Panel Visibility section
            main.Children.Add(Section("Panel Visibility"));
            _showToolbox = Check("Toolbox", _settings.ShowToolbox); main.Children.Add(_showToolbox);
            _showExplorer = Check("Explorer", _settings.ShowExplorer); main.Children.Add(_showExplorer);
            _showProperties = Check("Properties", _settings.ShowProperties); main.Children.Add(_showProperties);
            _showLayers = Check("Layers Panel", _settings.ShowLayersPanel); main.Children.Add(_showLayers);
            _showTemplates = Check("Templates", _settings.ShowTemplates); main.Children.Add(_showTemplates);

            main.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });

            // UI Sizing section
            main.Children.Add(Section("UI Sizing"));
            
            var fontRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            fontRow.Children.Add(new TextBlock { Text = "Font Size:", Width = 80, FontSize = 10 });
            _fontSlider = new Slider { Width = 120, Minimum = 6, Maximum = 10, Value = _settings.PanelFontSize, TickFrequency = 1, IsSnapToTickEnabled = true };
            _fontLabel = new TextBlock { Text = _settings.PanelFontSize.ToString("F0"), Width = 30, FontSize = 10 };
            _fontSlider.ValueChanged += (s, e) => _fontLabel.Text = _fontSlider.Value.ToString("F0");
            fontRow.Children.Add(_fontSlider);
            fontRow.Children.Add(_fontLabel);
            main.Children.Add(fontRow);

            var paddingRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            paddingRow.Children.Add(new TextBlock { Text = "Padding:", Width = 80, FontSize = 10 });
            _paddingSlider = new Slider { Width = 120, Minimum = 1, Maximum = 6, Value = _settings.PanelPadding, TickFrequency = 1, IsSnapToTickEnabled = true };
            _paddingLabel = new TextBlock { Text = _settings.PanelPadding.ToString("F0"), Width = 30, FontSize = 10 };
            _paddingSlider.ValueChanged += (s, e) => _paddingLabel.Text = _paddingSlider.Value.ToString("F0");
            paddingRow.Children.Add(_paddingSlider);
            paddingRow.Children.Add(_paddingLabel);
            main.Children.Add(paddingRow);

            var lineRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            lineRow.Children.Add(new TextBlock { Text = "Line Width:", Width = 80, FontSize = 10 });
            _lineSlider = new Slider { Width = 120, Minimum = 0.5, Maximum = 3, Value = _settings.LineThickness, TickFrequency = 0.5, IsSnapToTickEnabled = true };
            _lineLabel = new TextBlock { Text = _settings.LineThickness.ToString("F1"), Width = 30, FontSize = 10 };
            _lineSlider.ValueChanged += (s, e) => _lineLabel.Text = _lineSlider.Value.ToString("F1");
            lineRow.Children.Add(_lineSlider);
            lineRow.Children.Add(_lineLabel);
            main.Children.Add(lineRow);

            var pathRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            pathRow.Children.Add(new TextBlock { Text = "Path Width:", Width = 80, FontSize = 10 });
            _pathSlider = new Slider { Width = 120, Minimum = 1, Maximum = 4, Value = _settings.PathThickness, TickFrequency = 0.5, IsSnapToTickEnabled = true };
            _pathLabel = new TextBlock { Text = _settings.PathThickness.ToString("F1"), Width = 30, FontSize = 10 };
            _pathSlider.ValueChanged += (s, e) => _pathLabel.Text = _pathSlider.Value.ToString("F1");
            pathRow.Children.Add(_pathSlider);
            pathRow.Children.Add(_pathLabel);
            main.Children.Add(pathRow);

            // Buttons
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var okBtn = new Button { Content = "OK", Width = 70, Padding = new Thickness(0, 4, 0, 4) };
            var cancelBtn = new Button { Content = "Cancel", Width = 70, Padding = new Thickness(0, 4, 0, 4), Margin = new Thickness(8, 0, 0, 0) };
            okBtn.Click += (s, e) => { ApplySettings(); DialogResult = true; };
            cancelBtn.Click += (s, e) => DialogResult = false;
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            main.Children.Add(buttons);

            Content = main;
        }

        private TextBlock Section(string text) => new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4)
        };

        private CheckBox Check(string text, bool isChecked) => new CheckBox
        {
            Content = text,
            IsChecked = isChecked,
            FontSize = 10,
            Margin = new Thickness(0, 2, 0, 0)
        };

        private void ApplySettings()
        {
            _settings.ShowToolbox = _showToolbox.IsChecked ?? true;
            _settings.ShowExplorer = _showExplorer.IsChecked ?? true;
            _settings.ShowProperties = _showProperties.IsChecked ?? true;
            _settings.ShowLayersPanel = _showLayers.IsChecked ?? true;
            _settings.ShowTemplates = _showTemplates.IsChecked ?? true;
            _settings.PanelFontSize = _fontSlider.Value;
            _settings.PanelPadding = _paddingSlider.Value;
            _settings.LineThickness = _lineSlider.Value;
            _settings.PathThickness = _pathSlider.Value;
            _settings.Save();
        }
    }
}
