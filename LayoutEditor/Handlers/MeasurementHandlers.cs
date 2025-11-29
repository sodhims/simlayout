using System;
using System.Windows;
using System.Windows.Input;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for measurement and column tools
    /// </summary>
    public partial class MainWindow
    {
        #region Measurement State

        private bool _isDrawingMeasurement = false;
        private MeasurementData? _currentMeasurement = null;

        #endregion

        #region Column Tool

        private void ColumnTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "column";
            UpdateToolbarState();
            StatusText.Text = "Click to place column (Shift for round)";
        }

        private void HandleColumnClick(Point pos)
        {
            SaveUndoState();
            var snapped = SnapToGridPoint(pos);
            var column = new ColumnData
            {
                Id = Guid.NewGuid().ToString(),
                X = snapped.X,
                Y = snapped.Y,
                Width = 12,
                Height = 12,
                Shape = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "round" : "square"
            };
            _layout.Columns.Add(column);
            MarkDirty();
            Redraw();
            StatusText.Text = $"Placed {column.Shape} column (Shift+click for round)";
        }

        #endregion

        #region Measurement Tool

        private void MeasureTool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = "measure";
            UpdateToolbarState();
            StatusText.Text = "Measurement tool: Click two points to measure distance";
        }

        private void HandleMeasurementClick(Point pos)
        {
            if (!_isDrawingMeasurement)
            {
                // Start measurement
                var snapped = SnapToGridPoint(pos);
                _currentMeasurement = new MeasurementData
                {
                    Id = Guid.NewGuid().ToString(),
                    X1 = snapped.X,
                    Y1 = snapped.Y,
                    X2 = snapped.X,
                    Y2 = snapped.Y,
                    ShowLength = true
                };
                _layout.Measurements.Add(_currentMeasurement);
                _isDrawingMeasurement = true;
                StatusText.Text = "Click end point for measurement";
            }
            else
            {
                // End measurement
                if (_currentMeasurement != null)
                {
                    var endPos = GetConstrainedEndPoint(
                        new Point(_currentMeasurement.X1, _currentMeasurement.Y1),
                        pos,
                        Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                    
                    var snapped = SnapToGridPoint(endPos);
                    _currentMeasurement.X2 = snapped.X;
                    _currentMeasurement.Y2 = snapped.Y;

                    var lengthPx = _currentMeasurement.Length;
                    var lengthReal = lengthPx / _layout.Metadata.PixelsPerUnit;
                    StatusText.Text = $"Distance: {lengthReal:F2} {_layout.Metadata.Units} ({lengthPx:F0} px)";
                    MarkDirty();
                }
                
                _isDrawingMeasurement = false;
                _currentMeasurement = null;
                Redraw();
            }
        }

        private void UpdateMeasurementPreview(Point pos)
        {
            if (_isDrawingMeasurement && _currentMeasurement != null)
            {
                var endPos = GetConstrainedEndPoint(
                    new Point(_currentMeasurement.X1, _currentMeasurement.Y1),
                    pos,
                    Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                
                _currentMeasurement.X2 = endPos.X;
                _currentMeasurement.Y2 = endPos.Y;

                var lengthPx = _currentMeasurement.Length;
                var lengthReal = lengthPx / _layout.Metadata.PixelsPerUnit;
                StatusText.Text = $"Measuring: {lengthReal:F2} {_layout.Metadata.Units}";
                Redraw();
            }
        }

        private void CancelMeasurement()
        {
            if (_isDrawingMeasurement && _currentMeasurement != null)
            {
                _layout.Measurements.Remove(_currentMeasurement);
                _currentMeasurement = null;
                _isDrawingMeasurement = false;
                Redraw();
                StatusText.Text = "Measurement cancelled";
            }
        }

        private void ClearMeasurements_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Measurements.Count == 0)
            {
                StatusText.Text = "No measurements to clear";
                return;
            }

            SaveUndoState();
            _layout.Measurements.Clear();
            MarkDirty();
            Redraw();
            StatusText.Text = "All measurements cleared";
        }

        #endregion
    }
}
