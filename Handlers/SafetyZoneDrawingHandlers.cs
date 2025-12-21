using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for safety zone drawing operations
    /// </summary>
    public partial class MainWindow
    {
        #region Safety Zone Drawing State

        private bool _isDrawingSafetyZone = false;
        private SafetyZoneData? _currentSafetyZone = null;
        private string _currentSafetyZoneType = SafetyZoneTypes.KeepOut;

        #endregion

        #region Safety Zone Tool Selection

        private void SafetyZoneTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "safetyzone";
            _currentSafetyZoneType = SafetyZoneTypes.KeepOut;
            UpdateToolbarState();
            StatusText.Text = "Safety zone tool: Click to add boundary points, double-click or Enter to finish";
        }

        private void SafetyZoneTypeKeepOut_Click(object sender, RoutedEventArgs e)
        {
            _currentSafetyZoneType = SafetyZoneTypes.KeepOut;
            _currentTool = "safetyzone";
            StatusText.Text = "Drawing keep-out zones";
        }

        private void SafetyZoneTypeHardHat_Click(object sender, RoutedEventArgs e)
        {
            _currentSafetyZoneType = SafetyZoneTypes.HardHat;
            _currentTool = "safetyzone";
            StatusText.Text = "Drawing hard hat zones";
        }

        private void SafetyZoneTypeHighVis_Click(object sender, RoutedEventArgs e)
        {
            _currentSafetyZoneType = SafetyZoneTypes.HighVis;
            _currentTool = "safetyzone";
            StatusText.Text = "Drawing high visibility zones";
        }

        private void SafetyZoneTypeRestricted_Click(object sender, RoutedEventArgs e)
        {
            _currentSafetyZoneType = SafetyZoneTypes.Restricted;
            _currentTool = "safetyzone";
            StatusText.Text = "Drawing restricted zones";
        }

        #endregion

        #region Safety Zone Drawing

        private void HandleSafetyZoneClick(Point pos)
        {
            if (!_isDrawingSafetyZone)
            {
                // Start new safety zone
                SaveUndoState();
                var snapped = SnapToGridPoint(pos);
                _currentSafetyZone = new SafetyZoneData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{_currentSafetyZoneType} Zone {_layout.SafetyZones.Count + 1}",
                    ZoneType = _currentSafetyZoneType,
                    Color = GetSafetyZoneColor(_currentSafetyZoneType)
                };
                _currentSafetyZone.Boundary.Add(new PointData(snapped.X, snapped.Y));
                _layout.SafetyZones.Add(_currentSafetyZone);
                _isDrawingSafetyZone = true;
                StatusText.Text = "Click to add boundary points, double-click or Enter to finish (Esc to cancel)";
            }
            else
            {
                // Add point to safety zone boundary
                if (_currentSafetyZone != null)
                {
                    var snapped = SnapToGridPoint(pos);
                    _currentSafetyZone.Boundary.Add(new PointData(snapped.X, snapped.Y));
                    MarkDirty();
                    Redraw();
                }
            }
        }

        private void HandleSafetyZoneDoubleClick(Point pos)
        {
            // Finish safety zone on double-click
            FinishSafetyZone();
        }

        private void FinishSafetyZone()
        {
            if (_currentSafetyZone != null && _currentSafetyZone.Boundary.Count >= 3)
            {
                _isDrawingSafetyZone = false;
                _currentSafetyZone = null;
                MarkDirty();
                Redraw();
                StatusText.Text = "Safety zone completed. Click to start new zone.";
            }
            else if (_currentSafetyZone != null)
            {
                // Remove incomplete safety zone
                _layout.SafetyZones.Remove(_currentSafetyZone);
                _isDrawingSafetyZone = false;
                _currentSafetyZone = null;
                Redraw();
                StatusText.Text = "Safety zone needs at least 3 points. Cancelled.";
            }
        }

        private void CancelSafetyZone()
        {
            if (_currentSafetyZone != null)
            {
                _layout.SafetyZones.Remove(_currentSafetyZone);
                _currentSafetyZone = null;
            }
            _isDrawingSafetyZone = false;
            Redraw();
            StatusText.Text = "Safety zone cancelled";
        }

        private string GetSafetyZoneColor(string zoneType)
        {
            return zoneType switch
            {
                SafetyZoneTypes.KeepOut => "#FF0000",
                SafetyZoneTypes.HardHat => "#FFAA00",
                SafetyZoneTypes.HighVis => "#FFD700",
                SafetyZoneTypes.Restricted => "#FF6600",
                _ => "#CCCCCC"
            };
        }

        #endregion
    }
}
