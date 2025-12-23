using System;
using System.Windows;
using System.Windows.Threading;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Handles animations for frictionless mode demonstrations
    /// EOT cranes traverse their zone, Jib cranes rotate, AGVs travel paths
    /// </summary>
    public class AnimationService
    {
        private readonly DispatcherTimer _timer;
        private readonly LayoutData _layout;
        private readonly Action _redrawCallback;
        private readonly Action<string> _statusCallback;

        // Animation state
        private bool _isAnimating;
        private object? _animatingEntity;
        private double _animationProgress; // 0 to 1
        private bool _animationReversing;  // true = returning to start
        private double _animationSpeed = 0.02; // Progress per tick (adjustable)

        // Animation parameters
        private const double TickIntervalMs = 16; // ~60 FPS

        public bool IsAnimating => _isAnimating;
        public object? AnimatingEntity => _animatingEntity;

        public AnimationService(LayoutData layout, Action redrawCallback, Action<string> statusCallback)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _redrawCallback = redrawCallback ?? throw new ArgumentNullException(nameof(redrawCallback));
            _statusCallback = statusCallback ?? throw new ArgumentNullException(nameof(statusCallback));

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TickIntervalMs)
            };
            _timer.Tick += OnAnimationTick;
        }

        /// <summary>
        /// Start or stop animation for an EOT crane
        /// Clicking while animating stops the animation
        /// </summary>
        public bool ToggleEOTCraneAnimation(EOTCraneData crane)
        {
            if (crane == null)
            {
                DebugLogger.Log("[Animation] ToggleEOTCraneAnimation: crane is null");
                return false;
            }

            DebugLogger.Log($"[Animation] ToggleEOTCraneAnimation called for '{crane.Name}', _isAnimating={_isAnimating}");

            // If already animating this crane, stop it
            if (_isAnimating && _animatingEntity == crane)
            {
                DebugLogger.Log("[Animation] Stopping current EOT animation");
                StopAnimation();
                return false;
            }

            // If animating something else, stop that first
            if (_isAnimating)
            {
                DebugLogger.Log("[Animation] Stopping other animation first");
                StopAnimation();
            }

            // Start new animation
            _animatingEntity = crane;
            _animationProgress = 0;
            _animationReversing = false;
            _isAnimating = true;

            // Set speed based on crane's SpeedBridge property
            // Normalize: assume 1.0 speed = traverse in ~3 seconds (180 ticks at 60fps)
            _animationSpeed = Math.Max(0.005, crane.SpeedBridge * 0.02);

            // Store initial position to return to
            crane.AnimationStartPosition = crane.BridgePosition;

            DebugLogger.Log($"[Animation] Starting EOT animation: ZoneMin={crane.ZoneMin}, ZoneMax={crane.ZoneMax}, speed={_animationSpeed}");
            _statusCallback($"Animating EOT crane '{crane.Name}' - click to stop");
            _timer.Start();
            return true;
        }

        /// <summary>
        /// Start or stop animation for a Jib crane
        /// </summary>
        public bool ToggleJibCraneAnimation(JibCraneData crane)
        {
            if (crane == null) return false;

            // If already animating this crane, stop it
            if (_isAnimating && _animatingEntity == crane)
            {
                StopAnimation();
                return false;
            }

            // If animating something else, stop that first
            if (_isAnimating)
            {
                StopAnimation();
            }

            // Start new animation
            _animatingEntity = crane;
            _animationProgress = 0;
            _animationReversing = false;
            _isAnimating = true;

            // Set speed based on crane's speed property
            _animationSpeed = Math.Max(0.005, crane.Speed * 0.015);

            // Store initial angle
            crane.AnimationStartAngle = crane.CurrentAngle;

            _statusCallback($"Animating Jib crane '{crane.Name}' - click to stop");
            _timer.Start();
            return true;
        }

        /// <summary>
        /// Stop any running animation
        /// </summary>
        public void StopAnimation()
        {
            if (!_isAnimating) return;

            _timer.Stop();
            _isAnimating = false;

            // Reset entity to start position
            if (_animatingEntity is EOTCraneData eotCrane)
            {
                eotCrane.BridgePosition = eotCrane.AnimationStartPosition;
                _statusCallback($"EOT crane '{eotCrane.Name}' animation stopped");
            }
            else if (_animatingEntity is JibCraneData jibCrane)
            {
                jibCrane.CurrentAngle = jibCrane.AnimationStartAngle;
                _statusCallback($"Jib crane '{jibCrane.Name}' animation stopped");
            }

            _animatingEntity = null;
            _redrawCallback();
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (!_isAnimating || _animatingEntity == null)
            {
                _timer.Stop();
                return;
            }

            // Update progress
            if (_animationReversing)
            {
                _animationProgress -= _animationSpeed;
                if (_animationProgress <= 0)
                {
                    _animationProgress = 0;
                    _animationReversing = false;
                }
            }
            else
            {
                _animationProgress += _animationSpeed;
                if (_animationProgress >= 1)
                {
                    _animationProgress = 1;
                    _animationReversing = true;
                }
            }

            // Apply animation to entity
            if (_animatingEntity is EOTCraneData eotCrane)
            {
                AnimateEOTCrane(eotCrane);
            }
            else if (_animatingEntity is JibCraneData jibCrane)
            {
                AnimateJibCrane(jibCrane);
            }

            _redrawCallback();
        }

        private void AnimateEOTCrane(EOTCraneData crane)
        {
            // Interpolate between ZoneMin and ZoneMax
            double position = crane.ZoneMin + _animationProgress * (crane.ZoneMax - crane.ZoneMin);
            crane.BridgePosition = position;

            string direction = _animationReversing ? "←" : "→";
            _statusCallback($"EOT '{crane.Name}' {direction} {(position * 100):F0}% - click to stop");
        }

        private void AnimateJibCrane(JibCraneData crane)
        {
            // Interpolate between ArcStart and ArcEnd
            double angle = crane.ArcStart + _animationProgress * (crane.ArcEnd - crane.ArcStart);
            crane.CurrentAngle = angle;

            string direction = _animationReversing ? "↺" : "↻";
            _statusCallback($"Jib '{crane.Name}' {direction} {angle:F0}° - click to stop");
        }

        /// <summary>
        /// Check if a specific entity is currently animating
        /// </summary>
        public bool IsEntityAnimating(object entity)
        {
            return _isAnimating && _animatingEntity == entity;
        }

        /// <summary>
        /// Cleanup - stop animation and dispose timer
        /// </summary>
        public void Dispose()
        {
            StopAnimation();
            _timer.Tick -= OnAnimationTick;
        }
    }
}
