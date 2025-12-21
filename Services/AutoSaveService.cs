using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LayoutEditor.Data.Services;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for automatic saving and crash recovery
    /// Periodically saves the layout and maintains recovery snapshots
    /// </summary>
    public class AutoSaveService : IDisposable
    {
        private readonly LayoutService _layoutService;
        private Timer _autoSaveTimer;
        private LayoutData _currentLayout;
        private bool _isDirty = false;
        private bool _isEnabled = true;
        private int _autoSaveIntervalSeconds = 60; // Default 60 seconds
        private string _recoveryFilePath;
        private DateTime _lastSaveTime = DateTime.MinValue;
        private bool _disposed = false;

        /// <summary>
        /// Event raised when auto-save occurs
        /// </summary>
        public event EventHandler<AutoSaveEventArgs> AutoSaved;

        /// <summary>
        /// Event raised when auto-save fails
        /// </summary>
        public event EventHandler<AutoSaveErrorEventArgs> AutoSaveError;

        /// <summary>
        /// Auto-save interval in seconds
        /// </summary>
        public int AutoSaveIntervalSeconds
        {
            get => _autoSaveIntervalSeconds;
            set
            {
                _autoSaveIntervalSeconds = Math.Max(10, value); // Minimum 10 seconds
                RestartTimer();
            }
        }

        /// <summary>
        /// Enable/disable auto-save
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (value)
                    RestartTimer();
                else
                    StopTimer();
            }
        }

        /// <summary>
        /// Whether there are unsaved changes
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Time of last successful save
        /// </summary>
        public DateTime LastSaveTime => _lastSaveTime;

        public AutoSaveService(LayoutService layoutService)
        {
            _layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));

            // Set recovery file path in temp directory
            var tempPath = Path.GetTempPath();
            _recoveryFilePath = Path.Combine(tempPath, "layouteditor_recovery.json");
        }

        /// <summary>
        /// Start auto-save for a layout
        /// </summary>
        public void Start(LayoutData layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            _currentLayout = layout;
            _isDirty = false;

            // Start timer
            RestartTimer();
        }

        /// <summary>
        /// Stop auto-save
        /// </summary>
        public void Stop()
        {
            StopTimer();
            _currentLayout = null;
        }

        /// <summary>
        /// Mark layout as dirty (has unsaved changes)
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Manually trigger an auto-save
        /// </summary>
        public async Task<bool> SaveNowAsync()
        {
            if (_currentLayout == null || !_isDirty)
                return true;

            try
            {
                // Save to database
                bool saved = await _layoutService.SaveLayoutAsync(_currentLayout).ConfigureAwait(false);

                if (saved)
                {
                    _isDirty = false;
                    _lastSaveTime = DateTime.UtcNow;

                    // Also save recovery snapshot
                    await SaveRecoverySnapshotAsync().ConfigureAwait(false);

                    OnAutoSaved(new AutoSaveEventArgs
                    {
                        LayoutId = _currentLayout.Id,
                        SaveTime = _lastSaveTime,
                        IsManual = true
                    });

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnAutoSaveError(new AutoSaveErrorEventArgs
                {
                    Error = ex,
                    LayoutId = _currentLayout?.Id
                });
                return false;
            }
        }

        /// <summary>
        /// Save a recovery snapshot to temp file
        /// </summary>
        private async Task SaveRecoverySnapshotAsync()
        {
            if (_currentLayout == null)
                return;

            try
            {
                // Save layout to recovery file using JSON
                var json = System.Text.Json.JsonSerializer.Serialize(_currentLayout, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false
                });

                await File.WriteAllTextAsync(_recoveryFilePath, json);
            }
            catch
            {
                // Silently fail for recovery snapshot - don't disrupt main save
            }
        }

        /// <summary>
        /// Check if a recovery file exists
        /// </summary>
        public bool HasRecoveryFile()
        {
            return File.Exists(_recoveryFilePath);
        }

        /// <summary>
        /// Load layout from recovery file
        /// </summary>
        public async Task<LayoutData> LoadRecoveryAsync()
        {
            if (!HasRecoveryFile())
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(_recoveryFilePath);
                var layout = System.Text.Json.JsonSerializer.Deserialize<LayoutData>(json);
                return layout;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Delete the recovery file
        /// </summary>
        public void ClearRecovery()
        {
            try
            {
                if (File.Exists(_recoveryFilePath))
                {
                    File.Delete(_recoveryFilePath);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Get recovery file info
        /// </summary>
        public (bool exists, DateTime? modifiedTime) GetRecoveryInfo()
        {
            if (!File.Exists(_recoveryFilePath))
                return (false, null);

            try
            {
                var fileInfo = new FileInfo(_recoveryFilePath);
                return (true, fileInfo.LastWriteTimeUtc);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Timer callback for auto-save
        /// </summary>
        private async void AutoSaveCallback(object state)
        {
            if (!_isEnabled || _currentLayout == null || !_isDirty)
                return;

            try
            {
                // Save to database
                bool saved = await _layoutService.SaveLayoutAsync(_currentLayout).ConfigureAwait(false);

                if (saved)
                {
                    _isDirty = false;
                    _lastSaveTime = DateTime.UtcNow;

                    // Also save recovery snapshot
                    await SaveRecoverySnapshotAsync().ConfigureAwait(false);

                    OnAutoSaved(new AutoSaveEventArgs
                    {
                        LayoutId = _currentLayout.Id,
                        SaveTime = _lastSaveTime,
                        IsManual = false
                    });
                }
            }
            catch (Exception ex)
            {
                OnAutoSaveError(new AutoSaveErrorEventArgs
                {
                    Error = ex,
                    LayoutId = _currentLayout?.Id
                });
            }
        }

        /// <summary>
        /// Restart the auto-save timer
        /// </summary>
        private void RestartTimer()
        {
            StopTimer();

            if (_isEnabled)
            {
                var interval = TimeSpan.FromSeconds(_autoSaveIntervalSeconds);
                _autoSaveTimer = new Timer(AutoSaveCallback, null, interval, interval);
            }
        }

        /// <summary>
        /// Stop the auto-save timer
        /// </summary>
        private void StopTimer()
        {
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = null;
        }

        /// <summary>
        /// Raise the AutoSaved event
        /// </summary>
        private void OnAutoSaved(AutoSaveEventArgs e)
        {
            AutoSaved?.Invoke(this, e);
        }

        /// <summary>
        /// Raise the AutoSaveError event
        /// </summary>
        private void OnAutoSaveError(AutoSaveErrorEventArgs e)
        {
            AutoSaveError?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopTimer();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event args for auto-save completion
    /// </summary>
    public class AutoSaveEventArgs : EventArgs
    {
        public string LayoutId { get; set; }
        public DateTime SaveTime { get; set; }
        public bool IsManual { get; set; }
    }

    /// <summary>
    /// Event args for auto-save errors
    /// </summary>
    public class AutoSaveErrorEventArgs : EventArgs
    {
        public Exception Error { get; set; }
        public string LayoutId { get; set; }
    }
}
