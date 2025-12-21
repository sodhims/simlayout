using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for pedestrian crossing drawing operations
    /// </summary>
    public partial class MainWindow
    {
        #region Pedestrian Crossing Drawing State

        private bool _isDrawingCrossing = false;
        private PedestrianCrossingData? _currentCrossing = null;
        private string _currentCrossingType = PedestrianCrossingTypes.Zebra;

        #endregion

        #region Pedestrian Crossing Tool Selection

        private void CrossingTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "crossing";
            _currentCrossingType = PedestrianCrossingTypes.Zebra;
            UpdateToolbarState();
            StatusText.Text = "Crossing tool: Click to add points, double-click or Enter to finish";
        }

        private void CrossingTypeZebra_Click(object sender, RoutedEventArgs e)
        {
            _currentCrossingType = PedestrianCrossingTypes.Zebra;
            _currentTool = "crossing";
            StatusText.Text = "Drawing zebra crossings";
        }

        private void CrossingTypeSignal_Click(object sender, RoutedEventArgs e)
        {
            _currentCrossingType = PedestrianCrossingTypes.Signal;
            _currentTool = "crossing";
            StatusText.Text = "Drawing signal crossings";
        }

        private void CrossingTypeUnmarked_Click(object sender, RoutedEventArgs e)
        {
            _currentCrossingType = PedestrianCrossingTypes.Unmarked;
            _currentTool = "crossing";
            StatusText.Text = "Drawing unmarked crossings";
        }

        #endregion

        #region Pedestrian Crossing Drawing

        private void HandleCrossingClick(Point pos)
        {
            if (!_isDrawingCrossing)
            {
                // Start new crossing
                SaveUndoState();
                var snapped = SnapToGridPoint(pos);
                _currentCrossing = new PedestrianCrossingData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Crossing {_layout.PedestrianCrossings.Count + 1}",
                    CrossingType = _currentCrossingType,
                    Color = GetCrossingColor(_currentCrossingType)
                };
                _currentCrossing.Location.Add(new PointData(snapped.X, snapped.Y));
                _layout.PedestrianCrossings.Add(_currentCrossing);
                _isDrawingCrossing = true;
                StatusText.Text = "Click to add points, double-click or Enter to finish (Esc to cancel)";
            }
            else
            {
                // Add point to crossing
                if (_currentCrossing != null)
                {
                    var snapped = SnapToGridPoint(pos);
                    _currentCrossing.Location.Add(new PointData(snapped.X, snapped.Y));
                    MarkDirty();
                    Redraw();
                }
            }
        }

        private void HandleCrossingDoubleClick(Point pos)
        {
            // Finish crossing on double-click
            FinishCrossing();
        }

        private void FinishCrossing()
        {
            if (_currentCrossing != null && _currentCrossing.Location.Count >= 2)
            {
                _isDrawingCrossing = false;
                _currentCrossing = null;
                MarkDirty();
                Redraw();
                StatusText.Text = "Crossing completed. Click to start new crossing.";
            }
            else if (_currentCrossing != null)
            {
                // Remove incomplete crossing
                _layout.PedestrianCrossings.Remove(_currentCrossing);
                _isDrawingCrossing = false;
                _currentCrossing = null;
                Redraw();
                StatusText.Text = "Crossing needs at least 2 points. Cancelled.";
            }
        }

        private void CancelCrossing()
        {
            if (_currentCrossing != null)
            {
                _layout.PedestrianCrossings.Remove(_currentCrossing);
                _currentCrossing = null;
            }
            _isDrawingCrossing = false;
            Redraw();
            StatusText.Text = "Crossing cancelled";
        }

        private string GetCrossingColor(string crossingType)
        {
            return crossingType switch
            {
                PedestrianCrossingTypes.Zebra => "#FFFFFF",
                PedestrianCrossingTypes.Signal => "#FFD700",
                PedestrianCrossingTypes.Unmarked => "#CCCCCC",
                _ => "#FFFFFF"
            };
        }

        #endregion
    }
}
