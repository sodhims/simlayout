using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LayoutEditor.Transport
{
    /// <summary>
    /// Interface for transport network renderers
    /// </summary>
    public interface ITransportRenderer
    {
        /// <summary>
        /// Render the complete network to a canvas
        /// </summary>
        void Render(Canvas canvas);

        /// <summary>
        /// Clear all rendered elements
        /// </summary>
        void Clear(Canvas canvas);

        /// <summary>
        /// Hit test for selection
        /// </summary>
        object? HitTest(Point position);

        /// <summary>
        /// Highlight an element
        /// </summary>
        void Highlight(string elementId, bool highlight);

        /// <summary>
        /// Select an element
        /// </summary>
        void Select(string elementId, bool select);
    }

    /// <summary>
    /// Interface for transport path services
    /// </summary>
    public interface ITransportPathService
    {
        /// <summary>
        /// Create a loop connecting all points in a group
        /// </summary>
        void CreateLoopForGroup(string groupName);

        /// <summary>
        /// Add a blind/spur path from a point
        /// </summary>
        void AddBlindPath(string fromPointId, double offsetX, double offsetY);

        /// <summary>
        /// Connect a point to the nearest existing point
        /// </summary>
        void ConnectToNearest(string pointId);

        /// <summary>
        /// Calculate shortest path between two points
        /// </summary>
        string[]? FindPath(string fromId, string toId);
    }

    /// <summary>
    /// Common visual settings for transport rendering
    /// </summary>
    public class TransportVisualSettings
    {
        // Track visuals
        public double TrackWidth { get; set; } = 24;
        public double RailWidth { get; set; } = 2;
        public double RailSpacing { get; set; } = 8;
        
        // Chevron/direction indicators
        public double ChevronSpacing { get; set; } = 20;
        public double ChevronSize { get; set; } = 8;
        
        // Station visuals
        public double StationCornerRadius { get; set; } = 4;
        public double StationBorderWidth { get; set; } = 2;
        
        // Waypoint visuals
        public double WaypointRadius { get; set; } = 6;
        
        // Colors
        public Color TrackColor { get; set; } = Color.FromRgb(230, 126, 34);
        public Color StationColor { get; set; } = Color.FromRgb(155, 89, 182);
        public Color WaypointColor { get; set; } = Color.FromRgb(230, 126, 34);
        public Color SelectedColor { get; set; } = Color.FromRgb(52, 152, 219);
        public Color HighlightColor { get; set; } = Color.FromRgb(46, 204, 113);
        public Color BlockedColor { get; set; } = Color.FromRgb(231, 76, 60);
        
        // Opacity
        public double TrackBedOpacity { get; set; } = 0.3;
        public double StationFillOpacity { get; set; } = 0.3;
    }
}
