using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LayoutEditor.Models;

namespace LayoutEditor
{
    /// <summary>
    /// Handlers for background image operations
    /// </summary>
    public partial class MainWindow
    {
        #region Background Image Import

        private void ImportBackground_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Background Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bytes = File.ReadAllBytes(dialog.FileName);
                    var base64 = Convert.ToBase64String(bytes);

                    // Get image dimensions
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    SaveUndoState();
                    _layout.Background = new BackgroundImage
                    {
                        FilePath = dialog.FileName,
                        Base64Data = base64,
                        X = 0,
                        Y = 0,
                        Width = bitmap.PixelWidth,
                        Height = bitmap.PixelHeight,
                        Opacity = 0.4,
                        Scale = 1.0,
                        Locked = true
                    };

                    MarkDirty();
                    Redraw();
                    StatusText.Text = $"Imported background: {bitmap.PixelWidth}x{bitmap.PixelHeight}px";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import image: {ex.Message}", "Import Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Background Image Operations

        private void ClearBackground_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Background == null)
            {
                StatusText.Text = "No background image to clear";
                return;
            }

            SaveUndoState();
            _layout.Background = null;
            MarkDirty();
            Redraw();
            StatusText.Text = "Background image cleared";
        }

        private void BackgroundOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_layout?.Background != null)
            {
                _layout.Background.Opacity = e.NewValue;
                Redraw();
            }
        }

        private void ScaleBackground_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Background == null)
            {
                StatusText.Text = "No background image loaded";
                return;
            }

            // Simple scale options
            var result = MessageBox.Show(
                "Scale background?\n\nYes = 150%\nNo = 50%\nCancel = Keep current",
                "Scale Background",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            double factor = 1.0;
            if (result == MessageBoxResult.Yes)
                factor = 1.5;
            else if (result == MessageBoxResult.No)
                factor = 0.5;
            else
                return;

            SaveUndoState();
            _layout.Background.Scale *= factor;
            MarkDirty();
            Redraw();
            StatusText.Text = $"Background scaled to {_layout.Background.Scale:F2}x";
        }

        #endregion
    }
}
