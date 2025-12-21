using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor
{
    public partial class MainWindow
    {
        #region Crane Fields

        private CraneRenderer? _craneRenderer;
        private bool _isDrawingRunway = false;
        private Point? _runwayStartPoint = null;
        private bool _isPlacingJib = false;
        private Point? _jibCenterPoint = null;

        private RunwayData? _selectedRunway;
        private EOTCraneData? _selectedCrane;
        private JibCraneData? _selectedJib;
        
        private bool _isLoadingCraneProperties = false;

        #endregion

        #region Initialize Cranes

        private void InitializeCranes()
        {
            _craneRenderer = new CraneRenderer();
            EnsureCraneCollections();
        }

        private void EnsureCraneCollections()
        {
            if (_layout.Runways == null)
                _layout.Runways = new System.Collections.ObjectModel.ObservableCollection<RunwayData>();
            if (_layout.EOTCranes == null)
                _layout.EOTCranes = new System.Collections.ObjectModel.ObservableCollection<EOTCraneData>();
            if (_layout.JibCranes == null)
                _layout.JibCranes = new System.Collections.ObjectModel.ObservableCollection<JibCraneData>();
            if (_layout.HandoffPoints == null)
                _layout.HandoffPoints = new System.Collections.ObjectModel.ObservableCollection<HandoffPointData>();
        }

        #endregion

        #region Draw Crane Elements

        private void DrawCraneElements()
        {
            if (_craneRenderer == null) return;
            EnsureCraneCollections();

            _craneRenderer.Draw(EditorCanvas, _layout, (element, id, type) =>
            {
                _elementMap[id] = element;
            });
        }

        #endregion

        #region Runway Drawing

        private void BtnDrawRunway_Click(object sender, RoutedEventArgs e)
        {
            _isDrawingRunway = true;
            _runwayStartPoint = null;
            StatusText.Text = "Click and drag on canvas to draw runway";
            ModeText.Text = "RUNWAY";
            EditorCanvas.Cursor = Cursors.Cross;
        }

        private void HandleRunwayMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (!_isDrawingRunway) return;

            if (_runwayStartPoint == null)
            {
                _runwayStartPoint = position;
                StatusText.Text = "Drag to set runway end point, release to finish";
            }
        }

        private void HandleRunwayMouseMove(Point position)
        {
            if (!_isDrawingRunway || _runwayStartPoint == null) return;

            _craneRenderer?.DrawRunwayPreview(
                EditorCanvas,
                _runwayStartPoint.Value.X, _runwayStartPoint.Value.Y,
                position.X, position.Y);
        }

        private void HandleRunwayMouseUp(Point position, MouseButtonEventArgs e)
        {
            if (!_isDrawingRunway || _runwayStartPoint == null) return;

            var length = Math.Sqrt(
                Math.Pow(position.X - _runwayStartPoint.Value.X, 2) +
                Math.Pow(position.Y - _runwayStartPoint.Value.Y, 2));

            if (length < 20)
            {
                StatusText.Text = "Runway too short, try again";
                _craneRenderer?.ClearPreview(EditorCanvas);
                _runwayStartPoint = null;
                return;
            }

            SaveUndoState();
            EnsureCraneCollections();

            var runway = new RunwayData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Runway-{_layout.Runways!.Count + 1}",
                StartX = _runwayStartPoint.Value.X,
                StartY = _runwayStartPoint.Value.Y,
                EndX = position.X,
                EndY = position.Y
            };

            _layout.Runways.Add(runway);

            _isDrawingRunway = false;
            _runwayStartPoint = null;
            _craneRenderer?.ClearPreview(EditorCanvas);
            EditorCanvas.Cursor = Cursors.Arrow;
            ModeText.Text = "SELECT";

            RefreshRunwayList();
            MarkDirty();
            Redraw();

            StatusText.Text = $"Created runway: {runway.Name} (Length: {runway.Length:F1})";
        }

        private void CancelRunwayDrawing()
        {
            _isDrawingRunway = false;
            _runwayStartPoint = null;
            _craneRenderer?.ClearPreview(EditorCanvas);
            EditorCanvas.Cursor = Cursors.Arrow;
            ModeText.Text = "SELECT";
            StatusText.Text = "Ready";
        }

        #endregion

        #region Jib Crane Placement

        private void BtnPlaceJib_Click(object sender, RoutedEventArgs e)
        {
            _isPlacingJib = true;
            _jibCenterPoint = null;
            StatusText.Text = "Click to place jib crane center, drag for radius";
            ModeText.Text = "JIB";
            EditorCanvas.Cursor = Cursors.Cross;
        }

        private void HandleJibMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (!_isPlacingJib) return;

            if (_jibCenterPoint == null)
            {
                _jibCenterPoint = position;
                StatusText.Text = "Drag to set jib radius, release to finish";
            }
        }

        private void HandleJibMouseMove(Point position)
        {
            if (!_isPlacingJib || _jibCenterPoint == null) return;

            var radius = Math.Sqrt(
                Math.Pow(position.X - _jibCenterPoint.Value.X, 2) +
                Math.Pow(position.Y - _jibCenterPoint.Value.Y, 2));

            _craneRenderer?.DrawJibPreview(EditorCanvas,
                _jibCenterPoint.Value.X, _jibCenterPoint.Value.Y, radius);
        }

        private void HandleJibMouseUp(Point position, MouseButtonEventArgs e)
        {
            if (!_isPlacingJib || _jibCenterPoint == null) return;

            var radius = Math.Sqrt(
                Math.Pow(position.X - _jibCenterPoint.Value.X, 2) +
                Math.Pow(position.Y - _jibCenterPoint.Value.Y, 2));

            if (radius < 10)
            {
                StatusText.Text = "Jib radius too small, try again";
                _craneRenderer?.ClearPreview(EditorCanvas);
                _jibCenterPoint = null;
                return;
            }

            SaveUndoState();
            EnsureCraneCollections();

            var jib = new JibCraneData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Jib-{_layout.JibCranes!.Count + 1}",
                CenterX = _jibCenterPoint.Value.X,
                CenterY = _jibCenterPoint.Value.Y,
                Radius = radius,
                ArcStart = 0,
                ArcEnd = 360
            };

            _layout.JibCranes.Add(jib);

            _isPlacingJib = false;
            _jibCenterPoint = null;
            _craneRenderer?.ClearPreview(EditorCanvas);
            EditorCanvas.Cursor = Cursors.Arrow;
            ModeText.Text = "SELECT";

            RefreshJibList();
            MarkDirty();
            Redraw();

            StatusText.Text = $"Created jib crane: {jib.Name} (Radius: {jib.Radius:F1})";
        }

        private void CancelJibPlacement()
        {
            _isPlacingJib = false;
            _jibCenterPoint = null;
            _craneRenderer?.ClearPreview(EditorCanvas);
            EditorCanvas.Cursor = Cursors.Arrow;
            ModeText.Text = "SELECT";
            StatusText.Text = "Ready";
        }

        #endregion

        #region Runway List

        private void RunwayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRunway = RunwayList.SelectedItem as RunwayData;

            if (_selectedRunway != null)
            {
                _craneRenderer?.SelectRunway(_selectedRunway.Id);
                LoadRunwayProperties(_selectedRunway);
                RunwayPropsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _craneRenderer?.ClearSelection();
                RunwayPropsPanel.Visibility = Visibility.Collapsed;
            }

            Redraw();
        }

        private void LoadRunwayProperties(RunwayData runway)
        {
            _isLoadingCraneProperties = true;

            RunwayNameBox.Text = runway.Name;
            RunwayStartXBox.Text = runway.StartX.ToString("F1");
            RunwayStartYBox.Text = runway.StartY.ToString("F1");
            RunwayEndXBox.Text = runway.EndX.ToString("F1");
            RunwayEndYBox.Text = runway.EndY.ToString("F1");
            RunwayHeightBox.Text = runway.Height.ToString("F1");
            RunwayLengthText.Text = $"Length: {runway.Length:F1}";

            try
            {
                RunwayColorPreview.Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(runway.Color));
            }
            catch
            {
                RunwayColorPreview.Fill = Brushes.Gray;
            }

            _isLoadingCraneProperties = false;
        }

        private void RunwayProperty_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingCraneProperties || _selectedRunway == null) return;

            _selectedRunway.Name = RunwayNameBox.Text;

            if (double.TryParse(RunwayStartXBox.Text, out var sx))
                _selectedRunway.StartX = sx;
            if (double.TryParse(RunwayStartYBox.Text, out var sy))
                _selectedRunway.StartY = sy;
            if (double.TryParse(RunwayEndXBox.Text, out var ex))
                _selectedRunway.EndX = ex;
            if (double.TryParse(RunwayEndYBox.Text, out var ey))
                _selectedRunway.EndY = ey;
            if (double.TryParse(RunwayHeightBox.Text, out var h))
                _selectedRunway.Height = h;

            RunwayLengthText.Text = $"Length: {_selectedRunway.Length:F1}";

            RefreshRunwayList();
            MarkDirty();
            Redraw();
        }

        private void BtnDeleteRunway_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRunway == null) return;

            var cranesOnRunway = _layout.EOTCranes?.Where(c => c.RunwayId == _selectedRunway.Id).ToList();
            if (cranesOnRunway?.Count > 0)
            {
                var result = MessageBox.Show(
                    $"This runway has {cranesOnRunway.Count} crane(s). Delete them too?",
                    "Runway in Use",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes)
                {
                    SaveUndoState();
                    foreach (var crane in cranesOnRunway)
                        _layout.EOTCranes!.Remove(crane);

                    // Remove handoff points
                    var handoffs = _layout.HandoffPoints?.Where(h => h.RunwayId == _selectedRunway.Id).ToList();
                    if (handoffs != null)
                        foreach (var h in handoffs)
                            _layout.HandoffPoints!.Remove(h);
                }
                else
                {
                    return;
                }
            }
            else
            {
                SaveUndoState();
            }

            _layout.Runways!.Remove(_selectedRunway);
            _selectedRunway = null;

            RefreshRunwayList();
            RefreshCraneList();
            MarkDirty();
            Redraw();

            StatusText.Text = "Runway deleted";
        }

        private void RefreshRunwayList()
        {
            var selected = _selectedRunway;
            RunwayList.ItemsSource = null;
            RunwayList.ItemsSource = _layout.Runways;
            if (selected != null && _layout.Runways?.Contains(selected) == true)
                RunwayList.SelectedItem = selected;

            CraneRunwayCombo.ItemsSource = _layout.Runways;
        }

        #endregion

        #region EOT Crane List

        private void BtnAddCrane_Click(object sender, RoutedEventArgs e)
        {
            if (_layout.Runways == null || _layout.Runways.Count == 0)
            {
                MessageBox.Show("Create a runway first.", "No Runways",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveUndoState();
            EnsureCraneCollections();

            var runway = _selectedRunway ?? _layout.Runways.First();

            var crane = new EOTCraneData
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"EOT-{_layout.EOTCranes!.Count + 1}",
                RunwayId = runway.Id,
                ReachLeft = 50,
                ReachRight = 50,
                ZoneMin = 0,
                ZoneMax = 1
            };

            _layout.EOTCranes.Add(crane);

            RefreshCraneList();
            CraneList.SelectedItem = crane;
            MarkDirty();
            Redraw();

            StatusText.Text = $"Added crane: {crane.Name}";
            CheckForZoneOverlaps();
        }

        private void CraneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCrane = CraneList.SelectedItem as EOTCraneData;

            if (_selectedCrane != null)
            {
                _craneRenderer?.SelectCrane(_selectedCrane.Id);
                LoadCraneProperties(_selectedCrane);
                CranePropsPanel.Visibility = Visibility.Visible;
                CheckForZoneOverlaps();
            }
            else
            {
                _craneRenderer?.ClearSelection();
                CranePropsPanel.Visibility = Visibility.Collapsed;
                HandoffPanel.Visibility = Visibility.Collapsed;
            }

            Redraw();
        }

        private void LoadCraneProperties(EOTCraneData crane)
        {
            _isLoadingCraneProperties = true;

            CraneNameBox.Text = crane.Name;
            CraneRunwayCombo.SelectedValue = crane.RunwayId;
            CraneReachLeftBox.Text = crane.ReachLeft.ToString("F1");
            CraneReachRightBox.Text = crane.ReachRight.ToString("F1");
            CraneZoneMinSlider.Value = crane.ZoneMin * 100;
            CraneZoneMaxSlider.Value = crane.ZoneMax * 100;
            CraneSpeedBridgeBox.Text = crane.SpeedBridge.ToString("F2");
            CraneSpeedTrolleyBox.Text = crane.SpeedTrolley.ToString("F2");
            CraneSpeedHoistBox.Text = crane.SpeedHoist.ToString("F2");

            try
            {
                CraneColorPreview.Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(crane.Color));
            }
            catch
            {
                CraneColorPreview.Fill = Brushes.Orange;
            }

            _isLoadingCraneProperties = false;
        }

        private void CraneProperty_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingCraneProperties || _selectedCrane == null) return;

            _selectedCrane.Name = CraneNameBox.Text;

            if (double.TryParse(CraneReachLeftBox.Text, out var left))
                _selectedCrane.ReachLeft = left;
            if (double.TryParse(CraneReachRightBox.Text, out var right))
                _selectedCrane.ReachRight = right;
            if (double.TryParse(CraneSpeedBridgeBox.Text, out var bridge))
                _selectedCrane.SpeedBridge = bridge;
            if (double.TryParse(CraneSpeedTrolleyBox.Text, out var trolley))
                _selectedCrane.SpeedTrolley = trolley;
            if (double.TryParse(CraneSpeedHoistBox.Text, out var hoist))
                _selectedCrane.SpeedHoist = hoist;

            RefreshCraneList();
            MarkDirty();
            Redraw();
        }

        private void CraneRunway_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingCraneProperties || _selectedCrane == null) return;
            var runway = CraneRunwayCombo.SelectedItem as RunwayData;
            if (runway != null)
            {
                _selectedCrane.RunwayId = runway.Id;
                MarkDirty();
                Redraw();
                CheckForZoneOverlaps();
            }
        }

        private void CraneZone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isLoadingCraneProperties || _selectedCrane == null) return;

            _selectedCrane.ZoneMin = CraneZoneMinSlider.Value / 100;
            _selectedCrane.ZoneMax = CraneZoneMaxSlider.Value / 100;

            if (_selectedCrane.ZoneMin > _selectedCrane.ZoneMax)
            {
                var temp = _selectedCrane.ZoneMin;
                _selectedCrane.ZoneMin = _selectedCrane.ZoneMax;
                _selectedCrane.ZoneMax = temp;
            }

            MarkDirty();
            Redraw();
            CheckForZoneOverlaps();
        }

        private void BtnDeleteCrane_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCrane == null) return;

            SaveUndoState();

            var handoffsToRemove = _layout.HandoffPoints?
                .Where(h => h.Crane1Id == _selectedCrane.Id || h.Crane2Id == _selectedCrane.Id)
                .ToList();
            if (handoffsToRemove != null)
                foreach (var h in handoffsToRemove)
                    _layout.HandoffPoints!.Remove(h);

            _layout.EOTCranes!.Remove(_selectedCrane);
            _selectedCrane = null;

            RefreshCraneList();
            MarkDirty();
            Redraw();

            StatusText.Text = "Crane deleted";
            CheckForZoneOverlaps();
        }

        private void RefreshCraneList()
        {
            var selected = _selectedCrane;
            CraneList.ItemsSource = null;
            CraneList.ItemsSource = _layout.EOTCranes;
            if (selected != null && _layout.EOTCranes?.Contains(selected) == true)
                CraneList.SelectedItem = selected;
        }

        #endregion

        #region Zone Overlap / Handoff

        private void CheckForZoneOverlaps()
        {
            if (_selectedCrane == null || _layout.EOTCranes == null)
            {
                HandoffPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var otherCranes = _layout.EOTCranes
                .Where(c => c.Id != _selectedCrane.Id && c.RunwayId == _selectedCrane.RunwayId)
                .ToList();

            var overlappingCrane = otherCranes.FirstOrDefault(c =>
                CraneZoneCalculator.FindOverlap(_selectedCrane, c) != null);

            if (overlappingCrane != null)
            {
                HandoffPanel.Visibility = Visibility.Visible;
                RefreshHandoffList(_selectedCrane, overlappingCrane);

                var runway = _layout.Runways?.FirstOrDefault(r => r.Id == _selectedCrane.RunwayId);
                if (runway != null)
                    _craneRenderer?.DrawZoneOverlap(EditorCanvas, _selectedCrane, overlappingCrane, runway);
            }
            else
            {
                HandoffPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshHandoffList(EOTCraneData crane1, EOTCraneData crane2)
        {
            var handoffs = _layout.HandoffPoints?
                .Where(h =>
                    (h.Crane1Id == crane1.Id && h.Crane2Id == crane2.Id) ||
                    (h.Crane1Id == crane2.Id && h.Crane2Id == crane1.Id))
                .ToList();

            HandoffList.ItemsSource = handoffs;
        }

        private void BtnAddHandoff_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCrane == null) return;

            var otherCrane = _layout.EOTCranes?
                .FirstOrDefault(c => c.Id != _selectedCrane.Id &&
                    c.RunwayId == _selectedCrane.RunwayId &&
                    CraneZoneCalculator.FindOverlap(_selectedCrane, c) != null);

            if (otherCrane == null) return;

            var overlap = CraneZoneCalculator.FindOverlap(_selectedCrane, otherCrane);
            if (overlap == null) return;

            SaveUndoState();

            var handoff = new HandoffPointData
            {
                Id = Guid.NewGuid().ToString(),
                RunwayId = _selectedCrane.RunwayId,
                Crane1Id = _selectedCrane.Id,
                Crane2Id = otherCrane.Id,
                Position = (overlap.Value.min + overlap.Value.max) / 2,
                HandoffRule = "transfer"
            };

            _layout.HandoffPoints!.Add(handoff);

            RefreshHandoffList(_selectedCrane, otherCrane);
            MarkDirty();
            Redraw();
        }

        private void BtnRemoveHandoff_Click(object sender, RoutedEventArgs e)
        {
            var handoff = HandoffList.SelectedItem as HandoffPointData;
            if (handoff == null) return;

            SaveUndoState();
            _layout.HandoffPoints!.Remove(handoff);

            CheckForZoneOverlaps();
            MarkDirty();
            Redraw();
        }

        #endregion

        #region Jib Crane List

        private void JibList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedJib = JibList.SelectedItem as JibCraneData;

            if (_selectedJib != null)
            {
                _craneRenderer?.SelectJib(_selectedJib.Id);
                LoadJibProperties(_selectedJib);
                JibPropsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _craneRenderer?.ClearSelection();
                JibPropsPanel.Visibility = Visibility.Collapsed;
            }

            Redraw();
        }

        private void LoadJibProperties(JibCraneData jib)
        {
            _isLoadingCraneProperties = true;

            JibNameBox.Text = jib.Name;
            JibCenterXBox.Text = jib.CenterX.ToString("F1");
            JibCenterYBox.Text = jib.CenterY.ToString("F1");
            JibRadiusBox.Text = jib.Radius.ToString("F1");
            JibArcStartBox.Text = jib.ArcStart.ToString("F0");
            JibArcEndBox.Text = jib.ArcEnd.ToString("F0");
            JibSpeedSlewBox.Text = jib.SpeedSlew.ToString("F1");
            JibSpeedHoistBox.Text = jib.SpeedHoist.ToString("F2");

            try
            {
                JibColorPreview.Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(jib.Color));
            }
            catch
            {
                JibColorPreview.Fill = Brushes.Green;
            }

            _isLoadingCraneProperties = false;
        }

        private void JibProperty_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingCraneProperties || _selectedJib == null) return;

            _selectedJib.Name = JibNameBox.Text;

            if (double.TryParse(JibCenterXBox.Text, out var cx))
                _selectedJib.CenterX = cx;
            if (double.TryParse(JibCenterYBox.Text, out var cy))
                _selectedJib.CenterY = cy;
            if (double.TryParse(JibRadiusBox.Text, out var r))
                _selectedJib.Radius = r;
            if (double.TryParse(JibArcStartBox.Text, out var arcStart))
                _selectedJib.ArcStart = arcStart;
            if (double.TryParse(JibArcEndBox.Text, out var arcEnd))
                _selectedJib.ArcEnd = arcEnd;
            if (double.TryParse(JibSpeedSlewBox.Text, out var slew))
                _selectedJib.SpeedSlew = slew;
            if (double.TryParse(JibSpeedHoistBox.Text, out var hoist))
                _selectedJib.SpeedHoist = hoist;

            RefreshJibList();
            MarkDirty();
            Redraw();
        }

        private void BtnDeleteJib_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedJib == null) return;

            SaveUndoState();
            _layout.JibCranes!.Remove(_selectedJib);
            _selectedJib = null;

            RefreshJibList();
            MarkDirty();
            Redraw();

            StatusText.Text = "Jib crane deleted";
        }

        private void RefreshJibList()
        {
            var selected = _selectedJib;
            JibList.ItemsSource = null;
            JibList.ItemsSource = _layout.JibCranes;
            if (selected != null && _layout.JibCranes?.Contains(selected) == true)
                JibList.SelectedItem = selected;
        }

        #endregion

        #region Crane Mouse Integration

        /// <summary>
        /// Call from Canvas_MouseDown
        /// </summary>
        private bool HandleCraneMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (_isDrawingRunway)
            {
                HandleRunwayMouseDown(position, e);
                return true;
            }
            if (_isPlacingJib)
            {
                HandleJibMouseDown(position, e);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call from Canvas_MouseMove
        /// </summary>
        private bool HandleCraneMouseMove(Point position)
        {
            if (_isDrawingRunway && _runwayStartPoint != null)
            {
                HandleRunwayMouseMove(position);
                return true;
            }
            if (_isPlacingJib && _jibCenterPoint != null)
            {
                HandleJibMouseMove(position);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call from Canvas_MouseLeftButtonUp
        /// </summary>
        private bool HandleCraneMouseUp(Point position, MouseButtonEventArgs e)
        {
            if (_isDrawingRunway && _runwayStartPoint != null)
            {
                HandleRunwayMouseUp(position, e);
                return true;
            }
            if (_isPlacingJib && _jibCenterPoint != null)
            {
                HandleJibMouseUp(position, e);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call from KeyDown for Escape
        /// </summary>
        private bool HandleCraneKeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                if (_isDrawingRunway)
                {
                    CancelRunwayDrawing();
                    return true;
                }
                if (_isPlacingJib)
                {
                    CancelJibPlacement();
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
