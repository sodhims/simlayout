using System;
using System.Linq;
using System.Windows;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for recovering from problematic states
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Finds all nodes with negative or very large coordinates and moves them to visible area
        /// </summary>
        private void RecoverOffScreenNodes_Click(object sender, RoutedEventArgs e)
        {
            int recovered = 0;
            double margin = 100;
            double canvasWidth = EditorCanvas.Width;
            double canvasHeight = EditorCanvas.Height;
            
            SaveUndoState();
            
            foreach (var node in _layout.Nodes)
            {
                bool needsRecovery = false;
                double newX = node.Visual.X;
                double newY = node.Visual.Y;
                
                // Check if off-screen
                if (node.Visual.X < 0 || node.Visual.X > canvasWidth)
                {
                    newX = margin + (recovered % 10) * 50;
                    needsRecovery = true;
                }
                
                if (node.Visual.Y < 0 || node.Visual.Y > canvasHeight)
                {
                    newY = margin + (recovered / 10) * 50;
                    needsRecovery = true;
                }
                
                if (needsRecovery)
                {
                    node.Visual.X = newX;
                    node.Visual.Y = newY;
                    recovered++;
                }
            }
            
            // Also check transport stations
            if (_layout.TransportStations != null)
            {
                foreach (var station in _layout.TransportStations)
                {
                    bool needsRecovery = false;
                    double newX = station.Visual.X;
                    double newY = station.Visual.Y;
                    
                    if (station.Visual.X < 0 || station.Visual.X > canvasWidth)
                    {
                        newX = margin + (recovered % 10) * 50;
                        needsRecovery = true;
                    }
                    
                    if (station.Visual.Y < 0 || station.Visual.Y > canvasHeight)
                    {
                        newY = margin + (recovered / 10) * 50;
                        needsRecovery = true;
                    }
                    
                    if (needsRecovery)
                    {
                        station.Visual.X = newX;
                        station.Visual.Y = newY;
                        recovered++;
                    }
                }
            }
            
            if (recovered > 0)
            {
                MarkDirty();
                Redraw();
                MessageBox.Show($"Recovered {recovered} off-screen element(s).", "Recovery Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No off-screen elements found.", "Recovery", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// Reset canvas view to show all nodes (called by TransportGroupPanel)
        /// </summary>
        private void ResetViewToShowAllNodes()
        {
            // Just zoom to fit all content
            ZoomFit_Click(this, new RoutedEventArgs());
        }
    }
}
