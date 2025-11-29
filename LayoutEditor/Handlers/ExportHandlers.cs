using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LayoutEditor.Helpers;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        private ExportService? _exportService;

        private ExportService ExportService => _exportService ??= new ExportService();

        #region Export Operations

        private void ExportSimulation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|XML Files (*.xml)|*.xml",
                Title = "Export for Simulation",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonHelper.Serialize(_layout);
                    File.WriteAllText(dialog.FileName, json);
                    StatusText.Text = $"Exported: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                Title = "Export as Image",
                DefaultExt = ".png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExportCanvasToImage(dialog.FileName);
                    StatusText.Text = $"Exported: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SVG Files (*.svg)|*.svg",
                Title = "Export as SVG",
                DefaultExt = ".svg",
                FileName = $"{_layout.Metadata.Name}.svg"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var svg = ExportService.ExportToSvg(_layout);
                    File.WriteAllText(dialog.FileName, svg);
                    StatusText.Text = $"Exported SVG: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportDxf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "DXF Files (*.dxf)|*.dxf",
                Title = "Export as DXF (AutoCAD)",
                DefaultExt = ".dxf",
                FileName = $"{_layout.Metadata.Name}.dxf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var dxf = ExportService.ExportToDxf(_layout);
                    File.WriteAllText(dialog.FileName, dxf);
                    StatusText.Text = $"Exported DXF: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportBomCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Export Bill of Materials (CSV)",
                DefaultExt = ".csv",
                FileName = $"{_layout.Metadata.Name}_BOM.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var csv = ExportService.ExportBomToCsv(_layout);
                    File.WriteAllText(dialog.FileName, csv);
                    StatusText.Text = $"Exported BOM: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportEquipmentList_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Title = "Export Equipment List",
                DefaultExt = ".txt",
                FileName = $"{_layout.Metadata.Name}_Equipment.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var txt = ExportService.ExportEquipmentList(_layout);
                    File.WriteAllText(dialog.FileName, txt);
                    StatusText.Text = $"Exported equipment list: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportCanvasToImage(string filename)
        {
            // Create a render bitmap
            var bounds = GetContentBounds();
            var dpi = 96.0;

            var rtb = new RenderTargetBitmap(
                (int)(bounds.Width * 2),
                (int)(bounds.Height * 2),
                dpi * 2, dpi * 2,
                PixelFormats.Pbgra32);

            // Create visual to render
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(EditorCanvas)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, bounds.Width * 2, bounds.Height * 2));
                ctx.DrawRectangle(vb, null, new Rect(0, 0, bounds.Width * 2, bounds.Height * 2));
            }

            rtb.Render(dv);

            // Encode and save
            BitmapEncoder encoder;
            if (filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }

            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var stream = File.Create(filename);
            encoder.Save(stream);
        }

        private Rect GetContentBounds()
        {
            double minX = 0, minY = 0;
            double maxX = _layout.Canvas.Width, maxY = _layout.Canvas.Height;

            // Include nodes
            foreach (var node in _layout.Nodes)
            {
                maxX = Math.Max(maxX, node.Visual.X + node.Visual.Width + 50);
                maxY = Math.Max(maxY, node.Visual.Y + node.Visual.Height + 50);
            }

            // Include walls
            foreach (var wall in _layout.Walls)
            {
                maxX = Math.Max(maxX, Math.Max(wall.X1, wall.X2) + 50);
                maxY = Math.Max(maxY, Math.Max(wall.Y1, wall.Y2) + 50);
            }

            return new Rect(minX, minY, maxX, maxY);
        }

        private void ImportDxf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DXF import is planned for a future update.\n\nFor now, you can import background images and trace over them.", 
                "Import DXF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
