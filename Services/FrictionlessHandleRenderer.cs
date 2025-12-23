using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders visual handles (blinking dots) for entities that can be moved in frictionless mode
    /// </summary>
    public class FrictionlessHandleRenderer
    {
        private readonly LayoutData _layout;
        private bool _handleVisible = true;

        public FrictionlessHandleRenderer(LayoutData layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        /// <summary>
        /// Set handle visibility state (called by blinking timer)
        /// </summary>
        public void SetHandleVisibility(bool visible)
        {
            _handleVisible = visible;
        }

        /// <summary>
        /// Draw all handles for movable entities in frictionless mode
        /// </summary>
        public void DrawHandles(Canvas canvas)
        {
            if (!_layout.FrictionlessMode || !_handleVisible)
                return;

            // EOT Crane handles
            DrawEOTCraneHandles(canvas);

            // Jib Crane handles
            DrawJibCraneHandles(canvas);

            // Zone handles removed - zones shouldn't move in frictionless mode (simulation)
            // They can be edited in design mode instead

            // AGV Path handles (at waypoints)
            DrawAGVPathHandles(canvas);
        }

        #region Handle Drawing

        private void DrawEOTCraneHandles(Canvas canvas)
        {
            foreach (var crane in _layout.EOTCranes)
            {
                var runway = _layout.Runways?.FirstOrDefault(r => r.Id == crane.RunwayId);
                if (runway == null) continue;

                // Get crane's current position on runway
                var (x, y) = runway.GetPositionAt(crane.BridgePosition);

                // Draw handle at bridge position
                DrawHandle(canvas, x, y, Brushes.CornflowerBlue, Brushes.DarkBlue);
            }
        }

        private void DrawJibCraneHandles(Canvas canvas)
        {
            foreach (var crane in _layout.JibCranes)
            {
                // Draw handle at jib crane center (pivot point)
                DrawHandle(canvas, crane.CenterX, crane.CenterY, Brushes.MediumPurple, Brushes.DarkMagenta);

                // Optional: Also draw handle at current boom tip position
                // This would require adding a BoomAngle property to JibCraneData
            }
        }

        private void DrawZoneHandles(Canvas canvas)
        {
            foreach (var zone in _layout.Zones)
            {
                if (zone.Points == null || zone.Points.Count == 0)
                    continue;

                // Calculate zone center
                double centerX = zone.Points.Sum(p => p.X) / zone.Points.Count;
                double centerY = zone.Points.Sum(p => p.Y) / zone.Points.Count;

                DrawHandle(canvas, centerX, centerY, Brushes.LimeGreen, Brushes.DarkGreen);
            }
        }

        private void DrawAGVPathHandles(Canvas canvas)
        {
            // AGV waypoints are NOT movable in frictionless mode
            // They represent fixed infrastructure - use Test Vehicles to validate tracks
            // No handles drawn here to avoid confusion
        }

        /// <summary>
        /// Draw a single handle at the specified position
        /// </summary>
        private void DrawHandle(Canvas canvas, double x, double y, Brush fillColor, Brush strokeColor)
        {
            const double handleRadius = 12.0;  // Increased from 8.0
            const double strokeThickness = 3.0;  // Increased from 2.5

            // Outer circle (main handle)
            var outerCircle = new Ellipse
            {
                Width = handleRadius * 2,
                Height = handleRadius * 2,
                Fill = fillColor,
                Stroke = strokeColor,
                StrokeThickness = strokeThickness,
                Opacity = 1.0,  // Full opacity for maximum visibility
                IsHitTestVisible = false
            };

            Canvas.SetLeft(outerCircle, x - handleRadius);
            Canvas.SetTop(outerCircle, y - handleRadius);
            Canvas.SetZIndex(outerCircle, 10000);  // Ensure it's on top
            canvas.Children.Add(outerCircle);

            // Inner highlight (makes it look 3D and more visible)
            var innerCircle = new Ellipse
            {
                Width = handleRadius,
                Height = handleRadius,
                Fill = Brushes.White,
                Opacity = 0.7,  // Increased from 0.5 for better visibility
                IsHitTestVisible = false
            };

            Canvas.SetLeft(innerCircle, x - handleRadius / 2);
            Canvas.SetTop(innerCircle, y - handleRadius / 2);
            Canvas.SetZIndex(innerCircle, 10001);  // On top of outer circle
            canvas.Children.Add(innerCircle);

            // Add a strong glow effect for visibility
            outerCircle.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = ((SolidColorBrush)fillColor).Color,
                BlurRadius = 12,  // Increased from 8
                ShadowDepth = 0,
                Opacity = 0.9  // Increased from 0.6
            };
        }

        #endregion
    }
}
