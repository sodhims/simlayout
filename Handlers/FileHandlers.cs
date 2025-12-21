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

        private async void OpenFromDatabase_Click(object sender, RoutedEventArgs e)
{
    if (_isDirty)
    {
        var result = MessageBox.Show("Save changes?", "Unsaved Changes",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return;
        if (result == MessageBoxResult.Yes) { Save_Click(sender, e); if (_isDirty) return; }
    }

    var dialog = new Dialogs.PostgresImportDialog();
    dialog.Owner = this;

    if (dialog.ShowDialog() == true && dialog.LoadedLayout != null)
    {
        _layout = dialog.LoadedLayout;
        _currentFilePath = null;
        Title = $"Layout Editor - {dialog.LoadedLayoutName} (from database)";
        _isDirty = false;
        RefreshAll();
        ZoomFit_Click(sender, e);
        StatusText.Text = $"Loaded '{dialog.LoadedLayoutName}' from database";
    }
}

private void SaveToDatabase_Click(object sender, RoutedEventArgs e)
{
    var layoutName = !string.IsNullOrEmpty(_currentFilePath) 
        ? Path.GetFileNameWithoutExtension(_currentFilePath) 
        : "Untitled Layout";
    var dialog = new Dialogs.PostgresExportDialog(_layout, layoutName);
    dialog.Owner = this;
    dialog.ShowDialog();
}

private void ImportSqlFile_Click(object sender, RoutedEventArgs e)
{
    var dialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
        Title = "Import Layout from SQL File"
    };

    if (dialog.ShowDialog() == true)
    {
        try
        {
            var importer = new Services.SqlFileImporter();
            _layout = importer.ImportFromSqlFile(dialog.FileName);
            _currentFilePath = null;
            Title = $"Layout Editor - {System.IO.Path.GetFileNameWithoutExtension(dialog.FileName)}";
            _isDirty = false;
            RefreshAll();
            ZoomFit_Click(sender, e);
            StatusText.Text = $"Imported from {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing SQL file:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

private void OpenDatabaseFile_Click(object sender, RoutedEventArgs e)
{
    var dialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*",
        Title = "Open Layout Database"
    };

    if (dialog.ShowDialog() == true)
    {
        try
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Save changes to current layout?", "Unsaved Changes", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes) Save_Click(sender, e);
            }

            var service = new Services.SqliteLayoutService();
            var layouts = service.GetLayouts(dialog.FileName);
            
            if (layouts.Count == 0)
            {
                MessageBox.Show("No layouts found in database file.", "Empty Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _layout = service.LoadLayout(dialog.FileName, layouts[0].Id);
            _currentFilePath = null;
            Title = $"Layout Editor - {layouts[0].Name}";
            _isDirty = false;
            RefreshAll();
            ZoomFit_Click(sender, e);
            StatusText.Text = $"Loaded from {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

private void SaveAsDatabaseFile_Click(object sender, RoutedEventArgs e)
{
    var layoutName = !string.IsNullOrEmpty(_currentFilePath) 
        ? System.IO.Path.GetFileNameWithoutExtension(_currentFilePath) 
        : "layout";

    var dialog = new Microsoft.Win32.SaveFileDialog
    {
        Filter = "Database Files (*.db)|*.db",
        FileName = $"{layoutName}.db",
        Title = "Save as Database File"
    };

    if (dialog.ShowDialog() == true)
    {
        try
        {
            var service = new Services.SqliteLayoutService();
            service.SaveLayout(_layout, dialog.FileName, layoutName);
            StatusText.Text = $"Saved to {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

        #endregion
    }
}
