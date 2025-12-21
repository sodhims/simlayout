using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Renders path connections with different styles based on ConnectionType.
    /// Integrate this into your existing PathRenderer or use standalone.
    /// </summary>
    public class PathConnectionRenderer
    {
        #region Constants

        // Line spacing for double-line tracks
        private const double DoubleLineSpacing = 4.0;
        private const double DefaultLineThickness = 2.0;
        private const double SelectedLineThickness = 3.0;
        private const double HoveredLineThickness = 2.5;
        
        // Chevron settings
        private const double ChevronSpacing = 40.0;
        private const double ChevronSize = 6.0;
        
        // Conveyor settings
        private const double ConveyorWidth = 12.0;
        private const double ConveyorStripeSpacing = 8.0;
        
        // Flow settings
        private const double FlowDotSpacing = 12.0;
        
        // Z-Index layers
        private const int ZIndexPathBackground = 5;
        private const int ZIndexPathLine = 6;
        private const int ZIndexPathArrows = 7;
        private const int ZIndexPathPreview = 50;

        #endregion

        #region Main Draw Method

        /// <summary>
        /// Draw a path based on its connection type
        /// </summary>
        public void DrawPath(Canvas canvas, Point from, Point to, string connectionType, 
            bool bidirectional, bool isSelected, bool isHovered, string? pathId = null,
            Action<FrameworkElement, string>? registerElement = null)
        {
            switch (connectionType)
            {
                case ConnectionTypes.PartFlow:
                    DrawFlowPath(canvas, from, to, bidirectional, isSelected, isHovered, 
                        ParseColor("#888888"), pathId, registerElement);  // Gray
                    break;
                    
                case ConnectionTypes.ResourceBinding:
                    DrawDashedPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#3498DB"), pathId, registerElement);  // Blue dashed
                    break;
                    
                case ConnectionTypes.ControlSignal:
                    DrawDashDotPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#9B59B6"), pathId, registerElement);  // Purple dash-dot
                    break;
                    
                case ConnectionTypes.AGVTrack:
                    DrawTrackPath(canvas, from, to, bidirectional, isSelected, isHovered, 
                        ParseColor("#E67E22"), pathId, registerElement);  // Orange
                    break;
                    
                case ConnectionTypes.TTrack:
                    DrawTrackPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#3498DB"), pathId, registerElement);  // Blue
                    break;
                    
                case ConnectionTypes.Conveyor:
                    DrawConveyorPath(canvas, from, to, bidirectional, isSelected, isHovered, pathId, registerElement);
                    break;
                    
                case ConnectionTypes.General:
                    DrawTrackPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#95A5A6"), pathId, registerElement);  // Gray
                    break;
                    
                case ConnectionTypes.TransportLink:  // Legacy
                    DrawFlowPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#E67E22"), pathId, registerElement);  // Orange dotted
                    break;
                    
                default:
                    DrawFlowPath(canvas, from, to, bidirectional, isSelected, isHovered,
                        ParseColor("#888888"), pathId, registerElement);
                    break;
            }
        }

        #endregion

        #region Flow Path (Dotted Line)

        /// <summary>
        /// Draw logical flow connection - dotted line with arrow
        /// </summary>
        private void DrawFlowPath(Canvas canvas, Point from, Point to, bool bidirectional,
            bool isSelected, bool isHovered, Color color, string? pathId, 
            Action<FrameworkElement, string>? registerElement)
        {
            var thickness = isSelected ? SelectedLineThickness : (isHovered ? HoveredLineThickness : DefaultLineThickness);

            // Selection highlight
            if (isSelected)
            {
                DrawSelectionHighlight(canvas, from, to, 8);
            }

            // Main dotted line
            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pathId
            };
            Panel.SetZIndex(line, ZIndexPathLine);
            canvas.Children.Add(line);

            // Arrow(s)
            if (bidirectional)
            {
                DrawFlowArrow(canvas, from, to, color, 0.35);
                DrawFlowArrow(canvas, to, from, color, 0.35);
            }
            else
            {
                DrawFlowArrow(canvas, from, to, color, 0.5);
            }

            if (pathId != null)
                registerElement?.Invoke(line, pathId);
        }

        /// <summary>
        /// Draw dashed line path (for Resource Binding)
        /// </summary>
        private void DrawDashedPath(Canvas canvas, Point from, Point to, bool bidirectional,
            bool isSelected, bool isHovered, Color color, string? pathId,
            Action<FrameworkElement, string>? registerElement)
        {
            var thickness = isSelected ? SelectedLineThickness : (isHovered ? HoveredLineThickness : DefaultLineThickness);

            if (isSelected)
            {
                DrawSelectionHighlight(canvas, from, to, 8);
            }

            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pathId
            };
            Panel.SetZIndex(line, ZIndexPathLine);
            canvas.Children.Add(line);

            if (bidirectional)
            {
                DrawFlowArrow(canvas, from, to, color, 0.35);
                DrawFlowArrow(canvas, to, from, color, 0.35);
            }
            else
            {
                DrawFlowArrow(canvas, from, to, color, 0.5);
            }

            if (pathId != null)
                registerElement?.Invoke(line, pathId);
        }

        /// <summary>
        /// Draw dash-dot line path (for Control Signal)
        /// </summary>
        private void DrawDashDotPath(Canvas canvas, Point from, Point to, bool bidirectional,
            bool isSelected, bool isHovered, Color color, string? pathId,
            Action<FrameworkElement, string>? registerElement)
        {
            var thickness = isSelected ? SelectedLineThickness : (isHovered ? HoveredLineThickness : 1.5);

            if (isSelected)
            {
                DrawSelectionHighlight(canvas, from, to, 6);
            }

            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeDashArray = new DoubleCollection { 8, 3, 2, 3 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pathId
            };
            Panel.SetZIndex(line, ZIndexPathLine);
            canvas.Children.Add(line);

            // Smaller arrows for control signals
            if (bidirectional)
            {
                DrawSmallArrow(canvas, from, to, color, 0.35);
                DrawSmallArrow(canvas, to, from, color, 0.35);
            }
            else
            {
                DrawSmallArrow(canvas, from, to, color, 0.5);
            }

            if (pathId != null)
                registerElement?.Invoke(line, pathId);
        }

        private void DrawSmallArrow(Canvas canvas, Point from, Point to, Color color, double position)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 20) return;

            var arrowPos = new Point(
                from.X + dx * position,
                from.Y + dy * position
            );
            
            var arrowSize = 5.0;  // Smaller than flow arrows
            var angle = Math.Atan2(dy, dx);
            
            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(arrowSize, 0),
                    new Point(-arrowSize * 0.5, arrowSize * 0.5),
                    new Point(-arrowSize * 0.5, -arrowSize * 0.5)
                },
                Fill = new SolidColorBrush(color),
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform(angle * 180 / Math.PI),
                        new TranslateTransform(arrowPos.X, arrowPos.Y)
                    }
                },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(arrow, ZIndexPathArrows);
            canvas.Children.Add(arrow);
        }

        private void DrawFlowArrow(Canvas canvas, Point from, Point to, Color color, double position)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 20) return;

            var ux = dx / length;
            var uy = dy / length;
            
            var arrowPos = new Point(
                from.X + dx * position,
                from.Y + dy * position
            );
            
            var arrowSize = 8.0;
            var angle = Math.Atan2(dy, dx);
            
            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(arrowSize, 0),
                    new Point(-arrowSize * 0.5, arrowSize * 0.6),
                    new Point(-arrowSize * 0.5, -arrowSize * 0.6)
                },
                Fill = new SolidColorBrush(color),
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform(angle * 180 / Math.PI),
                        new TranslateTransform(arrowPos.X, arrowPos.Y)
                    }
                },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(arrow, ZIndexPathArrows);
            canvas.Children.Add(arrow);
        }

        #endregion

        #region Track Path (Double Line with Chevrons)

        /// <summary>
        /// Draw track path - double line with direction chevrons (AGV, Transport, General)
        /// </summary>
        private void DrawTrackPath(Canvas canvas, Point from, Point to, bool bidirectional,
            bool isSelected, bool isHovered, Color color, string? pathId, 
            Action<FrameworkElement, string>? registerElement)
        {
            var length = Distance(from, to);
            if (length < 1) return;

            // Unit vectors
            var ux = (to.X - from.X) / length;
            var uy = (to.Y - from.Y) / length;
            var px = -uy;  // Perpendicular
            var py = ux;

            var thickness = isSelected ? SelectedLineThickness : (isHovered ? HoveredLineThickness : DefaultLineThickness);
            var brush = new SolidColorBrush(color);

            // Selection highlight
            if (isSelected)
            {
                DrawDoubleLineSelection(canvas, from, to, ux, uy, px, py, DoubleLineSpacing);
            }

            // Draw two parallel lines
            var line1 = new Line
            {
                X1 = from.X + px * DoubleLineSpacing,
                Y1 = from.Y + py * DoubleLineSpacing,
                X2 = to.X + px * DoubleLineSpacing,
                Y2 = to.Y + py * DoubleLineSpacing,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            var line2 = new Line
            {
                X1 = from.X - px * DoubleLineSpacing,
                Y1 = from.Y - py * DoubleLineSpacing,
                X2 = to.X - px * DoubleLineSpacing,
                Y2 = to.Y - py * DoubleLineSpacing,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(line1, ZIndexPathLine);
            Panel.SetZIndex(line2, ZIndexPathLine);
            canvas.Children.Add(line1);
            canvas.Children.Add(line2);

            // Hit test area (transparent, between the lines)
            var hitArea = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = Brushes.Transparent,
                StrokeThickness = DoubleLineSpacing * 3,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pathId
            };
            Panel.SetZIndex(hitArea, ZIndexPathLine + 1);
            canvas.Children.Add(hitArea);

            // Draw chevrons
            var chevronCount = Math.Max(1, (int)(length / ChevronSpacing));
            var actualSpacing = length / (chevronCount + 1);

            for (int i = 1; i <= chevronCount; i++)
            {
                var t = i * actualSpacing;
                var cx = from.X + ux * t;
                var cy = from.Y + uy * t;

                if (bidirectional)
                {
                    // Alternate: ►◄►◄
                    bool pointForward = (i % 2 == 1);
                    DrawChevron(canvas, cx, cy, ux, uy, ChevronSize, color, pointForward);
                }
                else
                {
                    // All forward: ►►►
                    DrawChevron(canvas, cx, cy, ux, uy, ChevronSize, color, true);
                }
            }

            if (pathId != null)
                registerElement?.Invoke(hitArea, pathId);
        }

        private void DrawChevron(Canvas canvas, double cx, double cy, double ux, double uy,
            double size, Color color, bool pointForward)
        {
            var px = -uy;
            var py = ux;
            var dir = pointForward ? 1.0 : -1.0;

            var points = new PointCollection
            {
                new Point(cx - ux * size * 0.5 * dir + px * size * 0.5, cy - uy * size * 0.5 * dir + py * size * 0.5),
                new Point(cx + ux * size * 0.5 * dir, cy + uy * size * 0.5 * dir),
                new Point(cx - ux * size * 0.5 * dir - px * size * 0.5, cy - uy * size * 0.5 * dir - py * size * 0.5)
            };

            var chevron = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(chevron, ZIndexPathArrows);
            canvas.Children.Add(chevron);
        }

        private void DrawDoubleLineSelection(Canvas canvas, Point from, Point to, 
            double ux, double uy, double px, double py, double spacing)
        {
            var selectionBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            var selectionOffset = spacing + 3;

            var sel1 = new Line
            {
                X1 = from.X + px * selectionOffset,
                Y1 = from.Y + py * selectionOffset,
                X2 = to.X + px * selectionOffset,
                Y2 = to.Y + py * selectionOffset,
                Stroke = selectionBrush,
                StrokeThickness = SelectedLineThickness + 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Opacity = 0.5,
                IsHitTestVisible = false
            };

            var sel2 = new Line
            {
                X1 = from.X - px * selectionOffset,
                Y1 = from.Y - py * selectionOffset,
                X2 = to.X - px * selectionOffset,
                Y2 = to.Y - py * selectionOffset,
                Stroke = selectionBrush,
                StrokeThickness = SelectedLineThickness + 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Opacity = 0.5,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(sel1, ZIndexPathBackground);
            Panel.SetZIndex(sel2, ZIndexPathBackground);
            canvas.Children.Add(sel1);
            canvas.Children.Add(sel2);
        }

        #endregion

        #region Conveyor Path (Banded/Striped)

        /// <summary>
        /// Draw conveyor path - solid band with perpendicular stripes
        /// </summary>
        private void DrawConveyorPath(Canvas canvas, Point from, Point to, bool bidirectional,
            bool isSelected, bool isHovered, string? pathId, Action<FrameworkElement, string>? registerElement)
        {
            var color = ParseColor("#27AE60");  // Green
            var length = Distance(from, to);
            if (length < 1) return;

            var ux = (to.X - from.X) / length;
            var uy = (to.Y - from.Y) / length;
            var px = -uy;
            var py = ux;

            // Selection highlight
            if (isSelected)
            {
                DrawSelectionHighlight(canvas, from, to, ConveyorWidth + 6);
            }

            // Conveyor background (solid band)
            var bgLine = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B)),
                StrokeThickness = ConveyorWidth,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pathId
            };
            Panel.SetZIndex(bgLine, ZIndexPathBackground);
            canvas.Children.Add(bgLine);

            // Conveyor edges
            var edgeOffset = ConveyorWidth / 2 - 1;
            var edgeBrush = new SolidColorBrush(color);

            var edge1 = new Line
            {
                X1 = from.X + px * edgeOffset,
                Y1 = from.Y + py * edgeOffset,
                X2 = to.X + px * edgeOffset,
                Y2 = to.Y + py * edgeOffset,
                Stroke = edgeBrush,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            var edge2 = new Line
            {
                X1 = from.X - px * edgeOffset,
                Y1 = from.Y - py * edgeOffset,
                X2 = to.X - px * edgeOffset,
                Y2 = to.Y - py * edgeOffset,
                Stroke = edgeBrush,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(edge1, ZIndexPathLine);
            Panel.SetZIndex(edge2, ZIndexPathLine);
            canvas.Children.Add(edge1);
            canvas.Children.Add(edge2);

            // Draw stripes (perpendicular lines)
            var stripeCount = (int)(length / ConveyorStripeSpacing);
            var stripeBrush = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B));
            var stripeHalfWidth = ConveyorWidth / 2 - 2;

            for (int i = 1; i <= stripeCount; i++)
            {
                var t = i * ConveyorStripeSpacing;
                var cx = from.X + ux * t;
                var cy = from.Y + uy * t;

                var stripe = new Line
                {
                    X1 = cx + px * stripeHalfWidth,
                    Y1 = cy + py * stripeHalfWidth,
                    X2 = cx - px * stripeHalfWidth,
                    Y2 = cy - py * stripeHalfWidth,
                    Stroke = stripeBrush,
                    StrokeThickness = 2,
                    IsHitTestVisible = false
                };
                Panel.SetZIndex(stripe, ZIndexPathLine);
                canvas.Children.Add(stripe);
            }

            // Direction arrow(s) for conveyor
            if (bidirectional)
            {
                DrawConveyorArrow(canvas, from, to, color, 0.3);
                DrawConveyorArrow(canvas, to, from, color, 0.3);
            }
            else
            {
                DrawConveyorArrow(canvas, from, to, color, 0.5);
            }

            if (pathId != null)
                registerElement?.Invoke(bgLine, pathId);
        }

        private void DrawConveyorArrow(Canvas canvas, Point from, Point to, Color color, double position)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 30) return;

            var arrowPos = new Point(
                from.X + dx * position,
                from.Y + dy * position
            );
            var angle = Math.Atan2(dy, dx);

            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(6, 0),
                    new Point(-4, 5),
                    new Point(-4, -5)
                },
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1,
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform(angle * 180 / Math.PI),
                        new TranslateTransform(arrowPos.X, arrowPos.Y)
                    }
                },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(arrow, ZIndexPathArrows);
            canvas.Children.Add(arrow);
        }

        #endregion

        #region Preview Drawing

        /// <summary>
        /// Draw path preview while user is creating a connection
        /// </summary>
        public void DrawPathPreview(Canvas canvas, Point from, Point to, string connectionType, bool isValid = true)
        {
            RemovePreviewElements(canvas);

            var baseColor = isValid ? GetConnectionColor(connectionType) : Color.FromRgb(231, 76, 60);

            switch (connectionType)
            {
                case ConnectionTypes.PartFlow:
                case ConnectionTypes.ResourceBinding:
                case ConnectionTypes.ControlSignal:
                case ConnectionTypes.TransportLink:
                    DrawFlowPreview(canvas, from, to, baseColor);
                    break;
                    
                case ConnectionTypes.Conveyor:
                    DrawConveyorPreview(canvas, from, to, baseColor);
                    break;
                    
                default:  // AGVTrack, TTrack, General - all use track style
                    DrawTrackPreview(canvas, from, to, baseColor);
                    break;
            }

            // Distance label
            var dist = Distance(from, to);
            var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
            var distLabel = new TextBlock
            {
                Text = $"{dist:F0}px",
                FontSize = 10,
                Foreground = new SolidColorBrush(baseColor),
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                Padding = new Thickness(3, 1, 3, 1),
                Tag = "pathPreview",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(distLabel, mid.X - 15);
            Canvas.SetTop(distLabel, mid.Y - 20);
            Panel.SetZIndex(distLabel, ZIndexPathPreview);
            canvas.Children.Add(distLabel);
        }

        private Color GetConnectionColor(string connectionType)
        {
            return connectionType switch
            {
                ConnectionTypes.PartFlow => ParseColor("#888888"),
                ConnectionTypes.ResourceBinding => ParseColor("#3498DB"),
                ConnectionTypes.ControlSignal => ParseColor("#9B59B6"),
                ConnectionTypes.AGVTrack => ParseColor("#E67E22"),
                ConnectionTypes.TTrack => ParseColor("#3498DB"),
                ConnectionTypes.Conveyor => ParseColor("#27AE60"),
                ConnectionTypes.General => ParseColor("#95A5A6"),
                ConnectionTypes.TransportLink => ParseColor("#E67E22"),
                _ => ParseColor("#888888")
            };
        }

        private void DrawFlowPreview(Canvas canvas, Point from, Point to, Color color)
        {
            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 6, 4 },
                Tag = "pathPreview",
                IsHitTestVisible = false
            };
            Panel.SetZIndex(line, ZIndexPathPreview);
            canvas.Children.Add(line);
        }

        private void DrawTrackPreview(Canvas canvas, Point from, Point to, Color color)
        {
            var length = Distance(from, to);
            if (length < 1) return;

            var ux = (to.X - from.X) / length;
            var uy = (to.Y - from.Y) / length;
            var px = -uy;
            var py = ux;

            var brush = new SolidColorBrush(color);
            var dashArray = new DoubleCollection { 6, 3 };

            var line1 = new Line
            {
                X1 = from.X + px * DoubleLineSpacing,
                Y1 = from.Y + py * DoubleLineSpacing,
                X2 = to.X + px * DoubleLineSpacing,
                Y2 = to.Y + py * DoubleLineSpacing,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = dashArray,
                Tag = "pathPreview",
                IsHitTestVisible = false
            };

            var line2 = new Line
            {
                X1 = from.X - px * DoubleLineSpacing,
                Y1 = from.Y - py * DoubleLineSpacing,
                X2 = to.X - px * DoubleLineSpacing,
                Y2 = to.Y - py * DoubleLineSpacing,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = dashArray,
                Tag = "pathPreview",
                IsHitTestVisible = false
            };

            Panel.SetZIndex(line1, ZIndexPathPreview);
            Panel.SetZIndex(line2, ZIndexPathPreview);
            canvas.Children.Add(line1);
            canvas.Children.Add(line2);

            // Preview chevrons (bidirectional style by default)
            var chevronCount = Math.Max(1, (int)(length / ChevronSpacing));
            var actualSpacing = length / (chevronCount + 1);

            for (int i = 1; i <= chevronCount; i++)
            {
                var t = i * actualSpacing;
                var cx = from.X + ux * t;
                var cy = from.Y + uy * t;
                
                bool forward = (i % 2 == 1);
                DrawPreviewChevron(canvas, cx, cy, ux, uy, ChevronSize * 0.8, color, forward);
            }
        }

        private void DrawPreviewChevron(Canvas canvas, double cx, double cy, double ux, double uy,
            double size, Color color, bool pointForward)
        {
            var px = -uy;
            var py = ux;
            var dir = pointForward ? 1.0 : -1.0;

            var points = new PointCollection
            {
                new Point(cx - ux * size * 0.5 * dir + px * size * 0.5, cy - uy * size * 0.5 * dir + py * size * 0.5),
                new Point(cx + ux * size * 0.5 * dir, cy + uy * size * 0.5 * dir),
                new Point(cx - ux * size * 0.5 * dir - px * size * 0.5, cy - uy * size * 0.5 * dir - py * size * 0.5)
            };

            var chevron = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                StrokeLineJoin = PenLineJoin.Round,
                Tag = "pathPreview",
                IsHitTestVisible = false
            };

            Panel.SetZIndex(chevron, ZIndexPathPreview);
            canvas.Children.Add(chevron);
        }

        private void DrawConveyorPreview(Canvas canvas, Point from, Point to, Color color)
        {
            var bgLine = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)),
                StrokeThickness = ConveyorWidth,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                Tag = "pathPreview",
                IsHitTestVisible = false
            };
            Panel.SetZIndex(bgLine, ZIndexPathPreview);
            canvas.Children.Add(bgLine);

            var centerLine = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Tag = "pathPreview",
                IsHitTestVisible = false
            };
            Panel.SetZIndex(centerLine, ZIndexPathPreview);
            canvas.Children.Add(centerLine);
        }

        public void RemovePreviewElements(Canvas canvas)
        {
            var toRemove = canvas.Children.OfType<FrameworkElement>()
                .Where(e => e.Tag as string == "pathPreview")
                .ToList();
            foreach (var elem in toRemove)
            {
                canvas.Children.Remove(elem);
            }
        }

        #endregion

        #region Helpers

        private void DrawSelectionHighlight(Canvas canvas, Point from, Point to, double width)
        {
            var highlight = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                StrokeThickness = width,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0.4,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(highlight, ZIndexPathBackground);
            canvas.Children.Add(highlight);
        }

        private double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        private Color ParseColor(string colorStr)
        {
            try
            {
                if (!string.IsNullOrEmpty(colorStr) && colorStr.StartsWith("#"))
                {
                    return (Color)ColorConverter.ConvertFromString(colorStr);
                }
            }
            catch { }
            return Colors.Gray;
        }

        #endregion
    }
}
