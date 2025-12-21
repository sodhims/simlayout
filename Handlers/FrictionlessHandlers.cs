using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Frictionless Mode Rendering

        /// <summary>
        /// Draw constraint guides for all constrained entities when in frictionless mode
        /// </summary>
        private void DrawConstraintGuides()
        {
            if (_layout == null) return;

            var constrainedDragService = new ConstrainedDragService(_layout);

            // Draw guides for EOT cranes (runway constraints)
            foreach (var crane in _layout.EOTCranes)
            {
                var guide = constrainedDragService.GetConstraintGuide(crane);
                if (guide != null)
                {
                    var path = new Path
                    {
                        Data = guide,
                        Stroke = Brushes.CornflowerBlue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Opacity = 0.6
                    };
                    EditorCanvas.Children.Add(path);
                }
            }

            // Draw guides for Jib cranes (arc constraints)
            foreach (var crane in _layout.JibCranes)
            {
                var guide = constrainedDragService.GetConstraintGuide(crane);
                if (guide != null)
                {
                    var path = new Path
                    {
                        Data = guide,
                        Stroke = Brushes.MediumPurple,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Opacity = 0.6
                    };
                    EditorCanvas.Children.Add(path);
                }
            }

            // Draw guides for Conveyors (path constraints)
            foreach (var conveyor in _layout.Conveyors)
            {
                var guide = constrainedDragService.GetConstraintGuide(conveyor);
                if (guide != null)
                {
                    var path = new Path
                    {
                        Data = guide,
                        Stroke = Brushes.Orange,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Opacity = 0.6
                    };
                    EditorCanvas.Children.Add(path);
                }
            }

            // Draw guides for Zones (polygon constraints)
            foreach (var zone in _layout.Zones)
            {
                var guide = constrainedDragService.GetConstraintGuide(zone);
                if (guide != null)
                {
                    var path = new Path
                    {
                        Data = guide,
                        Stroke = Brushes.LimeGreen,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Opacity = 0.6
                    };
                    EditorCanvas.Children.Add(path);
                }
            }

            // Draw guides for AGV paths (linear constraints between waypoints)
            foreach (var agvPath in _layout.AGVPaths)
            {
                var guide = constrainedDragService.GetConstraintGuide(agvPath);
                if (guide != null)
                {
                    var path = new Path
                    {
                        Data = guide,
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Opacity = 0.6
                    };
                    EditorCanvas.Children.Add(path);
                }
            }
        }

        #endregion

        #region Frictionless Mode Visual Feedback

        private System.Windows.Shapes.Ellipse? _constraintSnapIndicator;

        /// <summary>
        /// Show snap indicator at constrained position during drag
        /// </summary>
        private void ShowConstraintSnapIndicator(System.Windows.Point position)
        {
            if (_constraintSnapIndicator == null)
            {
                _constraintSnapIndicator = new System.Windows.Shapes.Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.LimeGreen,
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = 2,
                    Opacity = 0.8
                };
            }

            // Position the indicator (center it on the point)
            Canvas.SetLeft(_constraintSnapIndicator, position.X - 6);
            Canvas.SetTop(_constraintSnapIndicator, position.Y - 6);

            // Add to canvas if not already there
            if (!EditorCanvas.Children.Contains(_constraintSnapIndicator))
            {
                EditorCanvas.Children.Add(_constraintSnapIndicator);
            }
        }

        /// <summary>
        /// Hide the constraint snap indicator
        /// </summary>
        private void HideConstraintSnapIndicator()
        {
            if (_constraintSnapIndicator != null && EditorCanvas.Children.Contains(_constraintSnapIndicator))
            {
                EditorCanvas.Children.Remove(_constraintSnapIndicator);
            }
        }

        #endregion

        #region Status Bar Enhancements

        /// <summary>
        /// Update status bar with frictionless mode information
        /// </summary>
        private void UpdateFrictionlessModeStatus()
        {
            if (_layout == null || !_layout.FrictionlessMode)
                return;

            int constrainedEntityCount = 0;
            constrainedEntityCount += _layout.EOTCranes.Count;
            constrainedEntityCount += _layout.JibCranes.Count;
            constrainedEntityCount += _layout.Conveyors.Count;
            constrainedEntityCount += _layout.Zones.Count;
            constrainedEntityCount += _layout.AGVPaths.Count;

            StatusText.Text = $"Frictionless Mode: {constrainedEntityCount} constrained entities - Press F to toggle";
        }

        #endregion
    }
}
