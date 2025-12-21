using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Handles conveyor drawing and editing
    /// </summary>
    public partial class MainWindow
    {
        // Conveyor drawing state
        private bool _isDrawingConveyor = false;
        private List<PointData> _conveyorPathPoints = new();
        private string? _conveyorStartNodeId;
        private string? _conveyorEndNodeId;

        /// <summary>
        /// Start drawing a new conveyor
        /// </summary>
        public void StartDrawingConveyor()
        {
            _isDrawingConveyor = true;
            _conveyorPathPoints.Clear();
            _conveyorStartNodeId = null;
            _conveyorEndNodeId = null;
            StatusText.Text = "Click to add conveyor points, double-click to finish. Press Esc to cancel.";
            Mouse.OverrideCursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancel conveyor drawing
        /// </summary>
        public void CancelDrawingConveyor()
        {
            _isDrawingConveyor = false;
            _conveyorPathPoints.Clear();
            _conveyorStartNodeId = null;
            _conveyorEndNodeId = null;
            Mouse.OverrideCursor = null;
            StatusText.Text = "Conveyor drawing canceled";
            Redraw();
        }

        /// <summary>
        /// Handle click while drawing conveyor
        /// </summary>
        private void HandleConveyorDrawingClick(Point canvasPoint, MouseButtonEventArgs e)
        {
            if (!_isDrawingConveyor) return;

            // Check if clicked on a node terminal for snapping
            var hitResult = _hitTestService.HitTest(_layout, canvasPoint);
            Point snapPoint = canvasPoint;

            // First click: try to snap to equipment output terminal
            if (_conveyorPathPoints.Count == 0 && hitResult.Type == Services.HitType.NodeTerminal)
            {
                if (hitResult.Node != null && hitResult.TerminalType == "output")
                {
                    _conveyorStartNodeId = hitResult.Node.Id;
                    snapPoint = TerminalHelper.GetNodeOutputTerminal(hitResult.Node);
                    StatusText.Text = $"Conveyor started from {hitResult.Node.Name}. Click to add points, double-click to finish.";
                }
            }
            // Last click: try to snap to equipment input terminal
            else if (_conveyorPathPoints.Count > 0 && hitResult.Type == Services.HitType.NodeTerminal)
            {
                if (hitResult.Node != null && hitResult.TerminalType == "input")
                {
                    _conveyorEndNodeId = hitResult.Node.Id;
                    snapPoint = TerminalHelper.GetNodeInputTerminal(hitResult.Node);
                }
            }

            // Add point to path
            _conveyorPathPoints.Add(new PointData(snapPoint.X, snapPoint.Y));

            // Update visual feedback
            Redraw();
            DrawConveyorPreview();

            StatusText.Text = $"Conveyor path: {_conveyorPathPoints.Count} points. Double-click to finish.";
        }

        /// <summary>
        /// Handle double-click to finish conveyor
        /// </summary>
        private void HandleConveyorDoubleClick(Point canvasPoint)
        {
            if (!_isDrawingConveyor || _conveyorPathPoints.Count < 2) return;

            SaveUndoState();

            // Create the conveyor
            var conveyor = new ConveyorData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Conveyor{_layout.Conveyors.Count + 1}",
                Path = new List<PointData>(_conveyorPathPoints),
                Width = 40,
                Speed = 0.5,
                ConveyorType = ConveyorTypes.Belt,
                Direction = ConveyorDirections.Forward,
                FromNodeId = _conveyorStartNodeId,
                ToNodeId = _conveyorEndNodeId
            };

            _layout.Conveyors.Add(conveyor);

            // Finish drawing
            _isDrawingConveyor = false;
            _conveyorPathPoints.Clear();
            _conveyorStartNodeId = null;
            _conveyorEndNodeId = null;
            Mouse.OverrideCursor = null;

            MarkDirty();
            Redraw();

            StatusText.Text = $"Conveyor created with {conveyor.Path.Count} points.";
        }

        /// <summary>
        /// Draw preview of conveyor being drawn
        /// </summary>
        private void DrawConveyorPreview()
        {
            if (_conveyorPathPoints.Count < 1) return;

            // Draw temporary preview line
            var polyline = new System.Windows.Shapes.Polyline
            {
                Stroke = System.Windows.Media.Brushes.Orange,
                StrokeThickness = 3,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                Opacity = 0.6
            };

            foreach (var point in _conveyorPathPoints)
            {
                polyline.Points.Add(new Point(point.X, point.Y));
            }

            EditorCanvas.Children.Add(polyline);

            // Draw point markers
            foreach (var point in _conveyorPathPoints)
            {
                var marker = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = System.Windows.Media.Brushes.Orange,
                    Stroke = System.Windows.Media.Brushes.DarkOrange,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(marker, point.X - 4);
                Canvas.SetTop(marker, point.Y - 4);
                EditorCanvas.Children.Add(marker);
            }
        }

        /// <summary>
        /// Menu handler to start conveyor tool
        /// </summary>
        private void DrawConveyor_Click(object sender, RoutedEventArgs e)
        {
            StartDrawingConveyor();
        }
    }
}
