using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using FactorySimulation.Core.Models;
using NodeNetwork.ViewModels;
using ReactiveUI;

namespace FactorySimulation.Configurator.Graph;

/// <summary>
/// View model for the entire BOM network/graph
/// </summary>
public class BomNetworkViewModel : NetworkViewModel
{
    private BomNodeViewModel? _selectedNode;
    private PartType? _rootPart;

    /// <summary>
    /// Currently selected node
    /// </summary>
    public BomNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    /// <summary>
    /// The root part for this BOM tree
    /// </summary>
    public PartType? RootPart
    {
        get => _rootPart;
        set => this.RaiseAndSetIfChanged(ref _rootPart, value);
    }

    /// <summary>
    /// All BOM nodes in the network
    /// </summary>
    public ObservableCollection<BomNodeViewModel> BomNodes { get; } = new();

    /// <summary>
    /// All connections in the network
    /// </summary>
    public ObservableCollection<BomConnectionViewModel> BomConnections { get; } = new();

    public BomNetworkViewModel()
    {
        // NodeNetwork's DeleteSelectedNodes is already defined - we'll use our own method
    }

    /// <summary>
    /// Delete selected nodes from the network
    /// </summary>
    public void DeleteSelected()
    {
        var nodesToDelete = Nodes.Items.Where(n => n.IsSelected).ToList();
        foreach (var node in nodesToDelete)
        {
            // Remove connections first
            var connectionsToRemove = Connections.Items
                .Where(c => c.Input.Parent == node || c.Output.Parent == node)
                .ToList();

            foreach (var conn in connectionsToRemove)
            {
                Connections.Remove(conn);
                if (conn is BomConnectionViewModel bomConn)
                    BomConnections.Remove(bomConn);
            }

            Nodes.Remove(node);
            if (node is BomNodeViewModel bomNode)
                BomNodes.Remove(bomNode);
        }
    }

    /// <summary>
    /// Clear all nodes and connections
    /// </summary>
    public void Clear()
    {
        Connections.Clear();
        Nodes.Clear();
        BomNodes.Clear();
        BomConnections.Clear();
        SelectedNode = null;
    }

    /// <summary>
    /// Add a BOM node to the network
    /// </summary>
    public BomNodeViewModel AddNode(PartType partType, BomNodeType nodeType, decimal quantity = 1)
    {
        var node = new BomNodeViewModel(partType, nodeType, quantity);
        Nodes.Add(node);
        BomNodes.Add(node);
        return node;
    }

    /// <summary>
    /// Connect two nodes (parent to child)
    /// </summary>
    public BomConnectionViewModel? ConnectNodes(BomNodeViewModel parent, BomNodeViewModel child, decimal quantity)
    {
        if (parent.ChildrenOutput == null || child.ParentInput == null)
            return null;

        var connection = new BomConnectionViewModel(this, child.ParentInput, parent.ChildrenOutput, quantity);
        Connections.Add(connection);
        BomConnections.Add(connection);
        return connection;
    }

    /// <summary>
    /// Build BOM tree from a part type and its BOM
    /// </summary>
    public void BuildFromBom(PartType rootPart, BillOfMaterials? bom, Dictionary<int, BillOfMaterials>? childBoms = null)
    {
        Clear();
        RootPart = rootPart;

        if (rootPart == null) return;

        // Create root node
        var rootNode = AddNode(rootPart, BomNodeType.Product);
        rootNode.Position = new System.Windows.Point(50, 200);

        if (bom?.Items == null || bom.Items.Count == 0) return;

        // Create child nodes and connections
        BuildChildNodes(rootNode, bom.Items, childBoms, 250, 50);
    }

    private void BuildChildNodes(
        BomNodeViewModel parentNode,
        List<BOMItem> items,
        Dictionary<int, BillOfMaterials>? allBoms,
        double startX,
        double startY)
    {
        double y = startY;
        double spacing = 80;

        foreach (var item in items)
        {
            if (item.ComponentPart == null) continue;

            var nodeType = BomNodeViewModel.FromPartCategory(item.ComponentPart.Category);
            var childNode = AddNode(item.ComponentPart, nodeType, item.Quantity);
            childNode.Position = new System.Windows.Point(startX, y);

            // Connect to parent
            ConnectNodes(parentNode, childNode, item.Quantity);

            // Recursively add children if this is a subassembly
            if (nodeType == BomNodeType.SubAssembly && allBoms != null)
            {
                if (allBoms.TryGetValue(item.ComponentPart.Id, out var childBom) && childBom.Items.Count > 0)
                {
                    double childStartY = y;
                    BuildChildNodes(childNode, childBom.Items, allBoms, startX + 200, childStartY);

                    // Adjust Y based on number of children
                    y += (childBom.Items.Count - 1) * spacing;
                }
            }

            y += spacing;
        }
    }

    /// <summary>
    /// Auto-layout nodes in a tree structure
    /// </summary>
    public void AutoLayout()
    {
        if (BomNodes.Count == 0) return;

        // Find root node (no parent input or parent input with no connections)
        var rootNode = BomNodes.FirstOrDefault(n => n.NodeType == BomNodeType.Product);
        if (rootNode == null)
            rootNode = BomNodes.FirstOrDefault();

        if (rootNode == null) return;

        // Layout tree from root
        var visited = new HashSet<BomNodeViewModel>();
        LayoutNode(rootNode, 50, 200, 200, 80, visited);
    }

    private double LayoutNode(
        BomNodeViewModel node,
        double x,
        double y,
        double xSpacing,
        double ySpacing,
        HashSet<BomNodeViewModel> visited)
    {
        if (visited.Contains(node)) return y;
        visited.Add(node);

        node.Position = new System.Windows.Point(x, y);

        // Find children (nodes connected from this node's output)
        var children = BomConnections
            .Where(c => c.Output.Parent == node)
            .Select(c => c.Input.Parent as BomNodeViewModel)
            .Where(n => n != null && !visited.Contains(n))
            .ToList();

        if (children.Count == 0) return y + ySpacing;

        double childY = y;
        foreach (var child in children)
        {
            childY = LayoutNode(child!, x + xSpacing, childY, xSpacing, ySpacing, visited);
        }

        // Center parent vertically relative to children
        if (children.Count > 1)
        {
            var firstChildY = children.First()!.Position.Y;
            var lastChildY = children.Last()!.Position.Y;
            node.Position = new System.Windows.Point(x, (firstChildY + lastChildY) / 2);
        }

        return childY;
    }
}
