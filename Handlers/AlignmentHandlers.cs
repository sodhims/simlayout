using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Alignment Handlers

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            // Check if we have multiple groups selected
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsLeft();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignLeft(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {nodes.Count} nodes to left";
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsRight();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignRight(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {nodes.Count} nodes to right";
        }

        private void AlignTop_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsTop();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignTop(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {nodes.Count} nodes to top";
        }

        private void AlignBottom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsBottom();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignBottom(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {nodes.Count} nodes to bottom";
        }

        private void AlignCenterH_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsCenterH();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignCenterHorizontal(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Centered {nodes.Count} nodes horizontally";
        }

        private void AlignCenterV_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService.HasMultipleGroups)
            {
                AlignGroupsCenterV();
                return;
            }
            
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.AlignCenterVertical(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Centered {nodes.Count} nodes vertically";
        }

        private void DistributeH_Click(object sender, RoutedEventArgs e)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 3) return;

            SaveUndoState();
            _alignmentService.DistributeHorizontally(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Distributed {nodes.Count} nodes horizontally";
        }

        private void DistributeV_Click(object sender, RoutedEventArgs e)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 3) return;

            SaveUndoState();
            _alignmentService.DistributeVertically(nodes, _layout);
            UpdatePathsAfterNodeMove(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Distributed {nodes.Count} nodes vertically";
        }

        private void SameWidth_Click(object sender, RoutedEventArgs e)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.MatchWidth(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Matched width for {nodes.Count} nodes";
        }

        private void SameHeight_Click(object sender, RoutedEventArgs e)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.MatchHeight(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Matched height for {nodes.Count} nodes";
        }

        private void SameSize_Click(object sender, RoutedEventArgs e)
        {
            var nodes = _selectionService.GetSelectedNodes(_layout);
            if (nodes.Count < 2) return;

            SaveUndoState();
            _alignmentService.MatchSize(nodes);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Matched size for {nodes.Count} nodes";
        }

        #region Group Alignment

        private (double minX, double minY, double maxX, double maxY) GetGroupBounds(GroupData group)
        {
            var memberNodes = group.Members
                .Select(id => _layout.Nodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null)
                .ToList();

            if (memberNodes.Count == 0)
                return (0, 0, 0, 0);

            double minX = memberNodes.Min(n => n!.Visual.X);
            double minY = memberNodes.Min(n => n!.Visual.Y);
            double maxX = memberNodes.Max(n => n!.Visual.X + n!.Visual.Width);
            double maxY = memberNodes.Max(n => n!.Visual.Y + n!.Visual.Height);

            return (minX, minY, maxX, maxY);
        }

        private void MoveGroup(GroupData group, double dx, double dy)
        {
            foreach (var memberId in group.Members)
            {
                var node = _layout.Nodes.FirstOrDefault(n => n.Id == memberId);
                if (node != null)
                {
                    node.Visual.X += dx;
                    node.Visual.Y += dy;
                }
            }
        }

        private void AlignGroupsLeft()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double targetX = bounds.Min(b => b.bounds.minX);

            foreach (var (group, b) in bounds)
            {
                double dx = targetX - b.minX;
                if (dx != 0) MoveGroup(group, dx, 0);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {groups.Count} groups to left";
        }

        private void AlignGroupsRight()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double targetX = bounds.Max(b => b.bounds.maxX);

            foreach (var (group, b) in bounds)
            {
                double dx = targetX - b.maxX;
                if (dx != 0) MoveGroup(group, dx, 0);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {groups.Count} groups to right";
        }

        private void AlignGroupsTop()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double targetY = bounds.Min(b => b.bounds.minY);

            foreach (var (group, b) in bounds)
            {
                double dy = targetY - b.minY;
                if (dy != 0) MoveGroup(group, 0, dy);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {groups.Count} groups to top";
        }

        private void AlignGroupsBottom()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double targetY = bounds.Max(b => b.bounds.maxY);

            foreach (var (group, b) in bounds)
            {
                double dy = targetY - b.maxY;
                if (dy != 0) MoveGroup(group, 0, dy);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Aligned {groups.Count} groups to bottom";
        }

        private void AlignGroupsCenterH()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double totalCenterY = bounds.Average(b => (b.bounds.minY + b.bounds.maxY) / 2);

            foreach (var (group, b) in bounds)
            {
                double currentCenterY = (b.minY + b.maxY) / 2;
                double dy = totalCenterY - currentCenterY;
                if (dy != 0) MoveGroup(group, 0, dy);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Centered {groups.Count} groups horizontally";
        }

        private void AlignGroupsCenterV()
        {
            var groups = _selectionService.GetSelectedGroups(_layout);
            if (groups.Count < 2) return;

            SaveUndoState();
            
            var bounds = groups.Select(g => (group: g, bounds: GetGroupBounds(g))).ToList();
            double totalCenterX = bounds.Average(b => (b.bounds.minX + b.bounds.maxX) / 2);

            foreach (var (group, b) in bounds)
            {
                double currentCenterX = (b.minX + b.maxX) / 2;
                double dx = totalCenterX - currentCenterX;
                if (dx != 0) MoveGroup(group, dx, 0);
            }

            MarkDirty();
            Redraw();
            StatusText.Text = $"Centered {groups.Count} groups vertically";
        }

        #endregion

        #region Path Update Helper

        /// <summary>
        /// Clear waypoints on paths connected to moved nodes so they re-route correctly
        /// </summary>
        private void UpdatePathsAfterNodeMove(IEnumerable<NodeData> movedNodes)
        {
            var movedIds = new HashSet<string>(movedNodes.Select(n => n.Id));
            
            foreach (var path in _layout.Paths)
            {
                // If path connects to any moved node, clear its waypoints
                if (movedIds.Contains(path.From) || movedIds.Contains(path.To))
                {
                    path.Visual.Waypoints.Clear();
                }
            }
        }

        #endregion

        #endregion
    }
}
