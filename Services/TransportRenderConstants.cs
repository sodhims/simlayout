using System.Windows.Media;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Constants for transport network rendering
    /// </summary>
    public static class TransportRenderConstants
    {
        #region Track Styling

        /// <summary>
        /// Default track width in pixels
        /// </summary>
        public const double DefaultTrackWidth = 30.0;

        /// <summary>
        /// Center line width for tracks
        /// </summary>
        public const double CenterLineWidth = 2.0;

        /// <summary>
        /// Width of direction arrows
        /// </summary>
        public const double ArrowWidth = 10.0;

        /// <summary>
        /// Height of direction arrows
        /// </summary>
        public const double ArrowHeight = 8.0;

        /// <summary>
        /// Spacing between direction arrows on long segments
        /// </summary>
        public const double ArrowSpacing = 80.0;

        /// <summary>
        /// Opacity for track background fill
        /// </summary>
        public const double TrackBackgroundOpacity = 0.25;

        /// <summary>
        /// Opacity for blocked track overlay
        /// </summary>
        public const double BlockedOverlayOpacity = 0.4;

        /// <summary>
        /// Track preview dash array
        /// </summary>
        public static readonly DoubleCollection PreviewDashArray = new() { 6, 3 };

        /// <summary>
        /// Unidirectional track dash array
        /// </summary>
        public static readonly DoubleCollection UnidirectionalDashArray = new() { 8, 4 };

        /// <summary>
        /// Lane marker dash array
        /// </summary>
        public static readonly DoubleCollection LaneMarkerDashArray = new() { 4, 4 };

        #endregion

        #region Station Styling

        /// <summary>
        /// Default station size
        /// </summary>
        public const double DefaultStationSize = 50.0;

        /// <summary>
        /// Station corner radius
        /// </summary>
        public const double StationCornerRadius = 6.0;

        /// <summary>
        /// Station border width
        /// </summary>
        public const double StationBorderWidth = 2.0;

        /// <summary>
        /// Selected station border width
        /// </summary>
        public const double StationSelectedBorderWidth = 3.0;

        /// <summary>
        /// Station background opacity
        /// </summary>
        public const double StationBackgroundOpacity = 0.4;

        /// <summary>
        /// Station icon size ratio (relative to station size)
        /// </summary>
        public const double StationIconRatio = 0.5;

        /// <summary>
        /// Station label font size
        /// </summary>
        public const double StationLabelFontSize = 9.0;

        /// <summary>
        /// Station type indicator font size
        /// </summary>
        public const double StationTypeIndicatorFontSize = 8.0;

        #endregion

        #region Waypoint Styling

        /// <summary>
        /// Waypoint outer circle size
        /// </summary>
        public const double WaypointOuterSize = 16.0;

        /// <summary>
        /// Waypoint inner dot size
        /// </summary>
        public const double WaypointInnerSize = 6.0;

        /// <summary>
        /// Junction waypoint outer size (larger)
        /// </summary>
        public const double JunctionWaypointSize = 22.0;

        /// <summary>
        /// Waypoint border width
        /// </summary>
        public const double WaypointBorderWidth = 2.0;

        /// <summary>
        /// Selected waypoint border width
        /// </summary>
        public const double WaypointSelectedBorderWidth = 3.0;

        /// <summary>
        /// Waypoint background opacity
        /// </summary>
        public const double WaypointBackgroundOpacity = 0.6;

        #endregion

        #region Transporter Styling

        /// <summary>
        /// Default transporter size
        /// </summary>
        public const double TransporterSize = 20.0;

        /// <summary>
        /// Transporter aspect ratio (height = size * ratio)
        /// </summary>
        public const double TransporterAspectRatio = 0.7;

        /// <summary>
        /// Transporter corner radius
        /// </summary>
        public const double TransporterCornerRadius = 3.0;

        /// <summary>
        /// Transporter label font size
        /// </summary>
        public const double TransporterLabelFontSize = 8.0;

        #endregion

        #region Hit Testing

        /// <summary>
        /// Maximum distance for snapping to a point when drawing tracks
        /// </summary>
        public const double SnapDistance = 30.0;

        /// <summary>
        /// Minimum distance to consider a segment for waypoint insertion
        /// </summary>
        public const double SegmentHitDistance = 15.0;

        #endregion

        #region Colors

        /// <summary>
        /// Default transport network color (orange)
        /// </summary>
        public static readonly Color DefaultNetworkColor = Color.FromRgb(230, 126, 34);

        /// <summary>
        /// Color for track preview while drawing
        /// </summary>
        public static readonly Color PreviewColor = Color.FromRgb(52, 152, 219);

        /// <summary>
        /// Color for invalid/error state
        /// </summary>
        public static readonly Color ErrorColor = Color.FromRgb(231, 76, 60);

        /// <summary>
        /// Color for blocked tracks
        /// </summary>
        public static readonly Color BlockedColor = Color.FromRgb(127, 140, 141);

        /// <summary>
        /// Color for snap indicator
        /// </summary>
        public static readonly Color SnapIndicatorColor = Color.FromRgb(46, 204, 113);

        /// <summary>
        /// Selection highlight color
        /// </summary>
        public static readonly Color SelectionColor = Color.FromRgb(52, 152, 219);

        #endregion

        #region Station Type Colors

        public static Color GetStationColor(string stationType)
        {
            return stationType switch
            {
                "pickup" => Color.FromRgb(39, 174, 96),    // Green
                "dropoff" => Color.FromRgb(231, 76, 60),   // Red
                "home" => Color.FromRgb(243, 156, 18),     // Orange
                "buffer" => Color.FromRgb(155, 89, 182),   // Purple
                "crossing" => Color.FromRgb(52, 152, 219), // Blue
                "charging" => Color.FromRgb(241, 196, 15), // Yellow
                "maintenance" => Color.FromRgb(127, 140, 141), // Gray
                _ => Color.FromRgb(155, 89, 182)           // Default purple
            };
        }

        public static string GetStationColorHex(string stationType)
        {
            return stationType switch
            {
                "pickup" => "#27AE60",
                "dropoff" => "#E74C3C",
                "home" => "#F39C12",
                "buffer" => "#9B59B6",
                "crossing" => "#3498DB",
                "charging" => "#F1C40F",
                "maintenance" => "#7F8C8D",
                _ => "#9B59B6"
            };
        }

        #endregion

        #region Z-Index Layers

        /// <summary>
        /// Z-index for track backgrounds (lowest)
        /// </summary>
        public const int ZIndexTrackBackground = 10;

        /// <summary>
        /// Z-index for track center lines
        /// </summary>
        public const int ZIndexTrackCenterLine = 11;

        /// <summary>
        /// Z-index for direction arrows
        /// </summary>
        public const int ZIndexArrows = 12;

        /// <summary>
        /// Z-index for waypoints
        /// </summary>
        public const int ZIndexWaypoints = 20;

        /// <summary>
        /// Z-index for stations
        /// </summary>
        public const int ZIndexStations = 25;

        /// <summary>
        /// Z-index for transporters
        /// </summary>
        public const int ZIndexTransporters = 30;

        /// <summary>
        /// Z-index for selection overlays
        /// </summary>
        public const int ZIndexSelection = 40;

        /// <summary>
        /// Z-index for preview elements (highest)
        /// </summary>
        public const int ZIndexPreview = 50;

        #endregion
    }
}
