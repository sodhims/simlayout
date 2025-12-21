using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LayoutEditor.Models;

namespace LayoutEditor.Dialogs
{
    public partial class IconEditorDialog : Window
    {
        private bool _settingIn = false;
        private bool _settingOut = false;

        public IconEditorDialog()
        {
            InitializeComponent();
            RefreshList();
        }

        private void RefreshList()
        {
            IconRegistry.Load();
            IconList.ItemsSource = IconRegistry.Icons;
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.gif;*.png;*.jpg;*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                var meta = IconRegistry.RegisterFromFile(dlg.FileName);
                if (meta != null)
                {
                    RefreshList();
                    IconList.SelectedItem = IconRegistry.Get(meta.Key);
                }
            }
        }

        private void IconList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconList.SelectedItem is IconMeta meta)
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, meta.File);
                if (File.Exists(file))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(file);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.DecodePixelWidth = meta.Width;
                        bmp.EndInit();
                        PreviewImage.Source = bmp;
                    }
                    catch { PreviewImage.Source = null; }
                }
            }
            else
            {
                PreviewImage.Source = null;
            }
            PreviewCanvas.Children.Clear();
        }

        private void SetIn_Click(object sender, RoutedEventArgs e)
        {
            _settingIn = true; _settingOut = false; PreviewCanvas.IsHitTestVisible = true;
        }

        private void SetOut_Click(object sender, RoutedEventArgs e)
        {
            _settingOut = true; _settingIn = false; PreviewCanvas.IsHitTestVisible = true;
        }

        private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(IconList.SelectedItem is IconMeta meta)) return;
            var pos = e.GetPosition(PreviewImage);
            double nx = pos.X / PreviewImage.ActualWidth;
            double ny = pos.Y / PreviewImage.ActualHeight;
            if (_settingIn)
            {
                meta.TerminalInNorm = new System.Windows.Point(nx, ny);
                _settingIn = false;
            }
            else if (_settingOut)
            {
                meta.TerminalOutNorm = new System.Windows.Point(nx, ny);
                _settingOut = false;
            }
            IconRegistry.Save();
            // show a small marker
            PreviewCanvas.Children.Clear();
            var marker = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = System.Windows.Media.Brushes.Orange };
            Canvas.SetLeft(marker, nx * PreviewCanvas.Width - 4);
            Canvas.SetTop(marker, ny * PreviewCanvas.Height - 4);
            PreviewCanvas.Children.Add(marker);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
