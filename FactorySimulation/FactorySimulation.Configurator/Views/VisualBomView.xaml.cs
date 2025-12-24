using System.Windows;
using System.Windows.Controls;
using FactorySimulation.Configurator.ViewModels;

namespace FactorySimulation.Configurator.Views;

public partial class VisualBomView : UserControl
{
    public VisualBomView()
    {
        InitializeComponent();
    }

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is VisualBomViewModel viewModel)
        {
            viewModel.LoadBomCommand.Execute(null);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is VisualBomViewModel viewModel)
        {
            viewModel.LoadBomCommand.Execute(null);
        }
    }

    private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
    {
        SetAllTreeViewItemsExpanded(BomTreeView, true);
    }

    private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
    {
        SetAllTreeViewItemsExpanded(BomTreeView, false);
    }

    private void SetAllTreeViewItemsExpanded(ItemsControl items, bool expand)
    {
        foreach (var item in items.Items)
        {
            if (items.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeItem)
            {
                treeItem.IsExpanded = expand;
                SetAllTreeViewItemsExpanded(treeItem, expand);
            }
        }
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is VisualBomViewModel viewModel)
        {
            viewModel.DeleteSelectedCommand.Execute(null);
        }
    }

    private void BomTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is VisualBomViewModel viewModel && e.NewValue is BomTreeNode node)
        {
            viewModel.SelectedItem = node;
        }
    }

    private void ComponentsListView_ColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is GridViewColumnHeader header && header.Tag is string propertyName)
        {
            if (DataContext is VisualBomViewModel viewModel)
            {
                viewModel.SortComponentsBy(propertyName);
            }
        }
    }
}
