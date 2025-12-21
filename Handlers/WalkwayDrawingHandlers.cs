using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for walkway drawing operations
    /// </summary>
    public partial class MainWindow
    {
        #region Walkway Drawing State

        private bool _isDrawingWalkway = false;
        private WalkwayData? _currentWalkway = null;
        private string _currentWalkwayType = WalkwayTypes.Primary;

        #endregion

        #region Walkway Tool Selection

        private void WalkwayTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "walkway";
            _currentWalkwayType = WalkwayTypes.Primary;
            UpdateToolbarState();
            StatusText.Text = "Walkway tool: Click to add points, double-click or press Enter to finish";
        }

        private void WalkwayTypePrimary_Click(object sender, RoutedEventArgs e)
        {
            _currentWalkwayType = WalkwayTypes.Primary;
            _currentTool = "walkway";
            StatusText.Text = "Drawing primary walkways";
        }

        private void WalkwayTypeSecondary_Click(object sender, RoutedEventArgs e)
        {
            _currentWalkwayType = WalkwayTypes.Secondary;
            _currentTool = "walkway";
            StatusText.Text = "Drawing secondary walkways";
        }

        private void WalkwayTypeEmergency_Click(object sender, RoutedEventArgs e)
        {
            _currentWalkwayType = WalkwayTypes.Emergency;
            _currentTool = "walkway";
            StatusText.Text = "Drawing emergency walkways";
        }

        #endregion

        #region Walkway Drawing

        private void HandleWalkwayClick(Point pos)
        {
            if (!_isDrawingWalkway)
            {
                // Start new walkway
                SaveUndoState();
                var snapped = SnapToGridPoint(pos);
                _currentWalkway = new WalkwayData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Walkway {_layout.Walkways.Count + 1}",
                    WalkwayType = _currentWalkwayType,
                    Width = 30, // Default width
                    Color = GetWalkwayColor(_currentWalkwayType)
                };
                _currentWalkway.Centerline.Add(new PointData(snapped.X, snapped.Y));
                _layout.Walkways.Add(_currentWalkway);
                _isDrawingWalkway = true;
                StatusText.Text = "Click to add points, double-click or Enter to finish (Esc to cancel)";
            }
            else
            {
                // Add point to walkway
                if (_currentWalkway != null)
                {
                    var snapped = SnapToGridPoint(pos);
                    _currentWalkway.Centerline.Add(new PointData(snapped.X, snapped.Y));
                    MarkDirty();
                    Redraw();
                }
            }
        }

        private void HandleWalkwayDoubleClick(Point pos)
        {
            // Finish walkway on double-click
            FinishWalkway();
        }

        private void FinishWalkway()
        {
            if (_currentWalkway != null && _currentWalkway.Centerline.Count >= 2)
            {
                _isDrawingWalkway = false;
                _currentWalkway = null;
                MarkDirty();
                Redraw();
                StatusText.Text = "Walkway completed. Click to start new walkway.";
            }
            else if (_currentWalkway != null)
            {
                // Remove incomplete walkway
                _layout.Walkways.Remove(_currentWalkway);
                _isDrawingWalkway = false;
                _currentWalkway = null;
                Redraw();
                StatusText.Text = "Walkway needs at least 2 points. Cancelled.";
            }
        }

        private void CancelWalkway()
        {
            if (_currentWalkway != null)
            {
                _layout.Walkways.Remove(_currentWalkway);
                _currentWalkway = null;
            }
            _isDrawingWalkway = false;
            Redraw();
            StatusText.Text = "Walkway cancelled";
        }

        private string GetWalkwayColor(string walkwayType)
        {
            return walkwayType switch
            {
                WalkwayTypes.Primary => "#4A90E2",
                WalkwayTypes.Secondary => "#7ED321",
                WalkwayTypes.Emergency => "#F5A623",
                _ => "#CCCCCC"
            };
        }

        #endregion
    }
}
