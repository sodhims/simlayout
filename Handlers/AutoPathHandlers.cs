using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    /// <summary>
    /// Handles automatic path creation when terminals touch and rubberband path drawing
    /// </summary>
    public partial class MainWindow
    {
        private AutoPathDetector? _autoPathDetector;
        
        // Rubberband path state
        private Line? _rubberbandLine;
        private bool _isDrawingRubberbandPath;
        
        #region Auto Path Detection
        
        /// <summary>
        /// Initialize the auto path detector (call after layout is loaded)
        /// </summary>
        private void InitializeAutoPathDetector()
        {
            _autoPathDetector = new AutoPathDetector(_layout);
        }
        
        /// <summary>
        /// Call this after nodes are dragged to check for touching terminals.
        /// This is the main entry point - call from mouse up after drag.
        /// </summary>
        public void RunAutoPathDetection()
        {
            if (_layout == null) return;
            
            if (_autoPathDetector == null)
                _autoPathDetector = new AutoPathDetector(_layout);
            
            var selectedNodes = _selectionService.GetSelectedNodes(_layout).ToList();
            if (selectedNodes.Count == 0) return;
            
            int pathsCreated = _autoPathDetector.DetectAndConnect(selectedNodes);
            
            if (pathsCreated > 0)
            {
                MarkDirty();
                Redraw();
                StatusText.Text = $"Auto-connected {pathsCreated} path(s)";
            }
        }
        
        /// <summary>
        /// Call this after placing a new node to auto-connect it
        /// </summary>
        public void RunAutoPathDetectionForNode(NodeData node)
        {
            if (_layout == null || node == null) return;
            
            if (_autoPathDetector == null)
                _autoPathDetector = new AutoPathDetector(_layout);
            
            int pathsCreated = _autoPathDetector.DetectAndConnectSingle(node);
            
            if (pathsCreated > 0)
            {
                MarkDirty();
                Redraw();
                StatusText.Text = $"Auto-connected {pathsCreated} path(s)";
            }
        }
        
        #endregion
        
        #region Rubberband Path Drawing
        
        /// <summary>
        /// Start drawing a rubberband path from a terminal
        /// </summary>
        private void StartRubberbandPath(NodeData fromNode, bool fromOutput)
        {
            _isDrawingRubberbandPath = true;
            
            // Get terminal position
            var startPos = fromOutput 
                ? TerminalHelper.GetNodeOutputTerminal(fromNode)
                : TerminalHelper.GetNodeInputTerminal(fromNode);
            
            // Create rubberband line
            _rubberbandLine = new Line
            {
                X1 = startPos.X,
                Y1 = startPos.Y,
                X2 = startPos.X,
                Y2 = startPos.Y,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new[] { 4.0, 2.0 }),
                IsHitTestVisible = false
            };
            
            Canvas.SetZIndex(_rubberbandLine, 9998);
            EditorCanvas.Children.Add(_rubberbandLine);
            
            StatusText.Text = $"Drawing path from {fromNode.Name} - click destination terminal or press Escape";
        }
        
        /// <summary>
        /// Update rubberband line endpoint during mouse move
        /// </summary>
        private void UpdateRubberbandPath(Point mousePos)
        {
            if (_rubberbandLine != null && _isDrawingRubberbandPath)
            {
                _rubberbandLine.X2 = mousePos.X;
                _rubberbandLine.Y2 = mousePos.Y;
            }
        }
        
        /// <summary>
        /// Complete or cancel rubberband path
        /// </summary>
        private void EndRubberbandPath(bool completed)
        {
            _isDrawingRubberbandPath = false;
            
            // Remove rubberband line
            if (_rubberbandLine != null)
            {
                EditorCanvas.Children.Remove(_rubberbandLine);
                _rubberbandLine = null;
            }
            
            if (!completed)
            {
                StatusText.Text = "Path drawing cancelled";
            }
        }
        
        #endregion
    }
}
