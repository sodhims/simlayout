using System.Windows;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Configurator.Views;

/// <summary>
/// Dialog for displaying BOM explosion (flattened multi-level view)
/// </summary>
public partial class BomExplosionDialog : Window
{
    public BomExplosionDialog(string parentPartNumber, List<BOMExplosionLine> explosionLines)
    {
        InitializeComponent();

        HeaderText.Text = $"BOM Explosion for {parentPartNumber}";
        ExplosionGrid.ItemsSource = explosionLines;

        var maxLevel = explosionLines.Count > 0 ? explosionLines.Max(l => l.Level) : 0;
        var uniqueParts = explosionLines.Select(l => l.PartNumber).Distinct().Count();
        SummaryText.Text = $"{explosionLines.Count} line(s), {uniqueParts} unique part(s), {maxLevel} level(s) deep";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
