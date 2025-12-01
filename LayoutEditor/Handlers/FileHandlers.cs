using System;
using System.Linq;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using LayoutEditor.Helpers;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region File Operations

        private void New_Click(object sender, RoutedEventArgs e)
        {
            CleanupFloatingPanels();
            if (_isDirty && !ConfirmDiscardChanges()) return;

            _layout = LayoutFactory.CreateDefault();
            _currentFilePath = null;
            _isDirty = false;

            _undoService.Clear();
            _selectionService.ClearSelection();

            RefreshAll();
            UpdateTitle();
            StatusText.Text = "New layout created";
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            CleanupFloatingPanels();
            if (_isDirty && !ConfirmDiscardChanges()) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Layout Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Layout"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void LoadFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var loaded = JsonHelper.Deserialize<LayoutData>(json);

                if (loaded != null)
                {
                    _layout = loaded;
                    _currentFilePath = filePath;
                    _isDirty = false;

                    _undoService.Clear();
                    _selectionService.ClearSelection();

                    // Initialize layers if needed (for older files)
                    if (_layout.LayerManager == null || _layout.LayerManager.Layers.Count == 0)
                    {
                        _layout.LayerManager = new Models.LayerManager();
                        _layout.LayerManager.InitializeDefaultLayers();
                    }


                    RefreshAll();
                    UpdateTitle();
                    StatusText.Text = $"Loaded: {Path.GetFileName(filePath)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                SaveAs_Click(sender, e);
            else
                SaveToFile(_currentFilePath);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Layout Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Layout",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveToFile(dialog.FileName);
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonHelper.Serialize(_layout);
                File.WriteAllText(filePath, json);

                _currentFilePath = filePath;
                _isDirty = false;

                UpdateTitle();
                StatusText.Text = $"Saved: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ConfirmDiscardChanges()
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to discard them?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CleanupFloatingPanels();
            if (_isDirty && !ConfirmDiscardChanges())
            {
                e.Cancel = true;
            }
        }

        #endregion
    }
}
