using System;
using System.Threading;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Dialogs
{
    public partial class OptimizeDialog : Window
    {
        private readonly LayoutData _layout;
        private readonly Action _onOptimizationComplete;
        private readonly OptimizationService _optimizationService;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isOptimizing;

        public OptimizeDialog(LayoutData layout, Action onOptimizationComplete)
        {
            InitializeComponent();
            _layout = layout;
            _onOptimizationComplete = onOptimizationComplete;
            _optimizationService = new OptimizationService();

            // Subscribe to events
            _optimizationService.ProgressChanged += OnProgressChanged;
            _optimizationService.OptimizationCompleted += OnOptimizationCompleted;
        }

        private async void Optimize_Click(object sender, RoutedEventArgs e)
        {
            if (_isOptimizing)
            {
                // Cancel optimization
                _cancellationTokenSource?.Cancel();
                OptimizeButton.Content = "Optimize";
                ProgressText.Text = "Cancelling...";
                return;
            }

            // Build options from UI
            var options = new OptimizationOptions
            {
                MaxGenerations = ParseInt(GenerationsBox.Text, 100),
                PopulationSize = ParseInt(PopulationBox.Text, 50),
                MutationRate = ParseDouble(MutationBox.Text, 0.1),
                CrossoverRate = ParseDouble(CrossoverBox.Text, 0.8),
                RespectZones = RespectZonesCheck.IsChecked ?? true,
                MaintainConnectivity = MaintainConnectivityCheck.IsChecked ?? true
            };

            // Set objective
            if (ObjectiveTravel.IsChecked == true)
                options.Objective = "minimize_travel";
            else if (ObjectiveThroughput.IsChecked == true)
                options.Objective = "maximize_throughput";
            else if (ObjectiveBalance.IsChecked == true)
                options.Objective = "balance_workload";
            else
                options.Objective = "custom";

            // Add constraints
            if (MinSpacingCheck.IsChecked == true)
                options.Constraints.Add("min_spacing");
            if (KeepCranesOnRunwaysCheck.IsChecked == true)
                options.Constraints.Add("cranes_on_runways");

            // Start optimization
            _isOptimizing = true;
            OptimizeButton.Content = "Cancel";
            CancelButton.IsEnabled = false;
            ProgressBar.Value = 0;
            ProgressText.Text = "Starting optimization...";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var result = await _optimizationService.OptimizeAsync(_layout, options, _cancellationTokenSource.Token);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Optimization complete!\n\n" +
                        $"Duration: {result.Duration.TotalSeconds:F1} seconds\n" +
                        $"{result.Message}\n\n" +
                        $"Note: Connect your genetic algorithm implementation to the OptimizationService for actual optimization.",
                        "Optimization Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _onOptimizationComplete?.Invoke();
                }
                else
                {
                    MessageBox.Show(result.Message, "Optimization", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Optimization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isOptimizing = false;
                OptimizeButton.Content = "Optimize";
                CancelButton.IsEnabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void OnProgressChanged(object? sender, OptimizationProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = e.ProgressPercentage;
                ProgressText.Text = e.Message;
            });
        }

        private void OnOptimizationCompleted(object? sender, OptimizationCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = 100;
                ProgressText.Text = e.Result.Success ? "Optimization complete" : "Optimization failed";
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            Close();
        }

        private static int ParseInt(string text, int defaultValue)
        {
            return int.TryParse(text, out var value) ? value : defaultValue;
        }

        private static double ParseDouble(string text, double defaultValue)
        {
            return double.TryParse(text, out var value) ? value : defaultValue;
        }

        protected override void OnClosed(EventArgs e)
        {
            _optimizationService.ProgressChanged -= OnProgressChanged;
            _optimizationService.OptimizationCompleted -= OnOptimizationCompleted;
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
