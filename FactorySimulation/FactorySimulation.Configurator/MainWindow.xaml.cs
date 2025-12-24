using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FactorySimulation.Configurator.ViewModels;
using FactorySimulation.Configurator.Views;
using FactorySimulation.Core.Models;
using FactorySimulation.Data;

namespace FactorySimulation.Configurator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database
        DatabaseConfiguration.Initialize();

        // Create and set ViewModel
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Load data when window loads
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Factory Configurator\nVersion 1.0\n\nA scenario configuration tool for Factory Simulation.",
            "About Factory Configurator",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void PartSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = PartSearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(searchTerm))
        {
            PartsListBox.ItemsSource = _viewModel.BomEditor.AllParts;
        }
        else
        {
            PartsListBox.ItemsSource = _viewModel.BomEditor.AllParts
                .Where(p => p.PartNumber.ToLowerInvariant().Contains(searchTerm) ||
                           p.Name.ToLowerInvariant().Contains(searchTerm))
                .ToList();
        }
    }

    private async void AddPart_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPartDialog();
        if (dialog.ShowDialog() == true)
        {
            var newPart = new PartType
            {
                PartNumber = dialog.PartNumber,
                Name = dialog.PartName,
                Description = dialog.Description,
                Category = dialog.Category,
                UnitOfMeasure = dialog.UnitOfMeasure
            };

            var (success, error) = await _viewModel.BomEditor.CreatePartAsync(newPart);
            if (success)
            {
                // Clear search and reset binding to show new part
                PartSearchBox.Text = string.Empty;
                PartsListBox.ItemsSource = _viewModel.BomEditor.AllParts;

                MessageBox.Show($"Part '{dialog.PartNumber}' created successfully.",
                    "Part Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(error ?? "Failed to create part.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportParts_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Parts (select one or more files)",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            try
            {
                int totalImported = 0;
                int skippedCount = 0;
                var allWarnings = new List<string>();
                var allErrors = new List<string>();
                bool cancelAll = false;

                // Import each file
                foreach (var filePath in dialog.FileNames)
                {
                    if (cancelAll) break;

                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    var fileName = System.IO.Path.GetFileName(filePath);
                    var format = _viewModel.ImportExportService.DetectFormat(json);

                    // For product files (NestedBom format), show preview dialog
                    if (format == FactorySimulation.Services.ImportFormat.NestedBom)
                    {
                        var previewData = await _viewModel.ImportExportService.BuildImportPreviewAsync(json);
                        if (previewData != null)
                        {
                            var previewVm = ConvertToPreviewViewModel(previewData);
                            var previewDialog = new Dialogs.ImportPreviewDialog(previewVm)
                            {
                                Owner = this,
                                Title = $"Import Preview - {fileName}"
                            };

                            previewDialog.ShowDialog();

                            switch (previewDialog.Result)
                            {
                                case Dialogs.ImportPreviewResult.Accept:
                                    // Get modified quantities and import
                                    var modifiedQtys = previewDialog.GetModifiedQuantities();
                                    var (success, error, count) = await _viewModel.ImportExportService
                                        .ImportWithModificationsAsync(json, modifiedQtys);

                                    if (success)
                                    {
                                        totalImported += count;
                                    }
                                    else if (error != null)
                                    {
                                        allErrors.Add($"{fileName}: {error}");
                                    }
                                    break;

                                case Dialogs.ImportPreviewResult.Skip:
                                    skippedCount++;
                                    break;

                                case Dialogs.ImportPreviewResult.Cancel:
                                    cancelAll = true;
                                    break;
                            }
                            continue;
                        }
                    }

                    // For non-product files (components, subassemblies), import directly
                    var (isValid, errors, warnings) = await _viewModel.ImportExportService.ValidateImportAsync(json);

                    if (warnings.Count > 0)
                    {
                        allWarnings.AddRange(warnings.Select(w => $"{fileName}: {w}"));
                    }

                    if (!isValid)
                    {
                        allErrors.AddRange(errors.Select(err => $"{fileName}: {err}"));
                        continue;
                    }

                    // Import the file
                    var (importSuccess, importError, importedCount) = await _viewModel.ImportExportService.ImportFromJsonAsync(json);

                    if (importSuccess)
                    {
                        totalImported += importedCount;
                    }
                    else if (importError != null)
                    {
                        allErrors.Add($"{fileName}: {importError}");
                    }
                }

                // Refresh parts list
                PartSearchBox.Text = string.Empty;
                await _viewModel.BomEditor.InitializeAsync();
                PartsListBox.ItemsSource = _viewModel.BomEditor.AllParts;

                // Build result message
                var message = cancelAll
                    ? "Import cancelled by user."
                    : $"Import completed.\n{totalImported} new parts imported from {dialog.FileNames.Length} file(s).";

                if (skippedCount > 0)
                {
                    message += $"\n{skippedCount} product(s) skipped.";
                }

                if (allErrors.Count > 0)
                {
                    message += $"\n\nErrors ({allErrors.Count}):\n{string.Join("\n", allErrors.Take(5))}";
                    if (allErrors.Count > 5)
                        message += $"\n... and {allErrors.Count - 5} more";
                }

                if (allWarnings.Count > 0 && allWarnings.Count <= 10)
                {
                    message += $"\n\nWarnings:\n{string.Join("\n", allWarnings.Take(5))}";
                    if (allWarnings.Count > 5)
                        message += $"\n... and {allWarnings.Count - 5} more";
                }

                MessageBox.Show(message,
                    cancelAll ? "Import Cancelled" : (allErrors.Count > 0 ? "Import Completed with Errors" : "Import Complete"),
                    MessageBoxButton.OK,
                    cancelAll ? MessageBoxImage.Information : (allErrors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private Dialogs.ImportPreviewViewModel ConvertToPreviewViewModel(Services.ImportPreviewData previewData)
    {
        var vm = new Dialogs.ImportPreviewViewModel
        {
            ProductId = previewData.ProductId,
            ProductName = previewData.ProductName,
            TotalParts = previewData.TotalParts,
            NewParts = previewData.NewParts,
            ExistingParts = previewData.ExistingParts
        };

        foreach (var node in previewData.RootNodes)
        {
            vm.PreviewTree.Add(ConvertToPreviewTreeNode(node));
        }

        return vm;
    }

    private Dialogs.PreviewTreeNode ConvertToPreviewTreeNode(Services.ImportPreviewNode node)
    {
        var treeNode = new Dialogs.PreviewTreeNode
        {
            PartNumber = node.PartNumber,
            PartName = node.PartName,
            Category = Enum.TryParse<Core.Models.PartCategory>(node.Category, out var cat) ? cat : Core.Models.PartCategory.Component,
            Quantity = node.Quantity,
            UnitOfMeasure = node.UnitOfMeasure,
            IsNew = node.IsNew,
            IsRoot = node.IsRoot
        };

        foreach (var child in node.Children)
        {
            treeNode.Children.Add(ConvertToPreviewTreeNode(child));
        }

        return treeNode;
    }

    private async void ImportPartsFromFolder_Click(object sender, RoutedEventArgs e)
    {
        // Use OpenFileDialog with Multiselect - user can select multiple files from one or more folders
        var dialog = new OpenFileDialog
        {
            Title = "Select JSON files to import (selects parent folder(s) for full import)",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            // Get unique parent folders from all selected files
            var folders = dialog.FileNames
                .Select(f => System.IO.Path.GetDirectoryName(f))
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct()
                .ToList();

            if (folders.Count == 0)
            {
                MessageBox.Show("Could not determine folder path.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int totalImported = 0;
                var allErrors = new List<string>();

                foreach (var folderPath in folders!)
                {
                    var (success, error, importedCount) = await _viewModel.ImportExportService.ImportFromFolderAsync(folderPath!);
                    totalImported += importedCount;

                    if (!success && error != null)
                    {
                        allErrors.Add($"{System.IO.Path.GetFileName(folderPath)}: {error}");
                    }
                    else if (error != null)
                    {
                        // Warnings from successful import
                        allErrors.Add(error);
                    }
                }

                // Refresh parts list
                PartSearchBox.Text = string.Empty;
                await _viewModel.BomEditor.InitializeAsync();
                PartsListBox.ItemsSource = _viewModel.BomEditor.AllParts;

                var message = $"Import completed.\n{totalImported} new parts imported from {folders.Count} folder(s).";
                if (allErrors.Count > 0)
                {
                    message += $"\n\n{string.Join("\n", allErrors.Take(5))}";
                    if (allErrors.Count > 5)
                        message += $"\n... and {allErrors.Count - 5} more";
                }

                MessageBox.Show(message, "Import Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExportParts_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Parts",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"parts_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _viewModel.ImportExportService.ExportToFileAsync(dialog.FileName);

                MessageBox.Show(
                    $"Parts exported successfully to:\n{dialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
