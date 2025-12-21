using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Base class for floating tool panels with VS-style appearance
    /// </summary>
    public class FloatingPanel : Window
    {
        protected MainWindow? _mainWindow;
        private bool _forceClose = false;
        
        public FloatingPanel()
        {
            // VS-style floating panel appearance
            WindowStyle = WindowStyle.ToolWindow;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            
            // Default size
            Width = 250;
            Height = 400;
        }
        
        public void SetOwner(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            // Owner set after window is shown to avoid error
        }
        
        /// <summary>
        /// True if the panel has been permanently closed (not just hidden)
        /// </summary>
        public bool IsClosed => _forceClose;
        
        /// <summary>
        /// Force close the panel (used when app is exiting)
        /// </summary>
        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_forceClose)
            {
                // Hide instead of close (so we can re-show)
                e.Cancel = true;
                Hide();
            }
            base.OnClosing(e);
        }
        
        public void ToggleVisibility()
        {
            if (IsVisible)
                Hide();
            else
            {
                Show();
                Activate();
            }
        }
        
        /// <summary>
        /// Position the panel relative to the main window
        /// </summary>
        public void PositionRelativeTo(MainWindow main, HorizontalAlignment hAlign, VerticalAlignment vAlign, double offsetX = 0, double offsetY = 0)
        {
            double x = main.Left;
            double y = main.Top;
            
            switch (hAlign)
            {
                case HorizontalAlignment.Left:
                    x = main.Left + offsetX;
                    break;
                case HorizontalAlignment.Right:
                    x = main.Left + main.Width - Width + offsetX;
                    break;
                case HorizontalAlignment.Center:
                    x = main.Left + (main.Width - Width) / 2 + offsetX;
                    break;
            }
            
            switch (vAlign)
            {
                case VerticalAlignment.Top:
                    y = main.Top + offsetY + 80; // Below toolbar
                    break;
                case VerticalAlignment.Bottom:
                    y = main.Top + main.Height - Height + offsetY - 30;
                    break;
                case VerticalAlignment.Center:
                    y = main.Top + (main.Height - Height) / 2 + offsetY;
                    break;
            }
            
            Left = x;
            Top = y;
        }
        
        /// <summary>
        /// Creates a standard panel header
        /// </summary>
        protected Border CreateHeader(string title)
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCE, 0xDB)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            
            var text = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 8,
                FontFamily = new FontFamily("Segoe UI"),
                Padding = new Thickness(6, 4, 6, 4)
            };
            
            header.Child = text;
            return header;
        }
    }
}
