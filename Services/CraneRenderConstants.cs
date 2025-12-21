using System.Windows.Media;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Crane-specific rendering constants - ADD these to your existing RenderConstants.cs
    /// or keep as separate static class
    /// </summary>
    public static class CraneRenderConstants
    {
        // Runway constants
        public const double RunwayLineWidth = 6;
        public const double RunwaySelectedWidth = 8;
        public const double RunwayRailOffset = 3;
        public const double RunwayRailWidth = 2;
        public const double RunwayEndMarkerSize = 10;

        // EOT Crane constants
        public const double CraneEnvelopeStrokeWidth = 2;
        public const double CraneEnvelopeSelectedWidth = 3;
        public const double CraneBridgeWidth = 4;
        public const double CraneTrolleyWidth = 12;
        public const double CraneTrolleyHeight = 8;
        public const byte CraneEnvelopeFillAlpha = 40;

        // Jib crane constants
        public const double JibCraneArcWidth = 2;
        public const double JibCranePivotSize = 12;
        public const byte JibCraneFillAlpha = 30;

        // Handoff constants
        public const double HandoffMarkerSize = 12;
    }

    /// <summary>
    /// Crane color utilities
    /// </summary>
    public static class CraneColors
    {
        public static Color DefaultRunway => Color.FromRgb(100, 100, 100);
        public static Color DefaultCrane => Color.FromRgb(255, 140, 0);
        public static Color DefaultJib => Color.FromRgb(60, 179, 113);
        public static Color HandoffPoint => Color.FromRgb(255, 215, 0);
        public static Color HandoffStroke => Color.FromRgb(180, 140, 0);
        public static Color OverlapZone => Color.FromArgb(60, 255, 165, 0);
        public static Color Selected => Color.FromRgb(0, 120, 215);

        public static SolidColorBrush GetBrush(Color color) => new SolidColorBrush(color);
        public static SolidColorBrush RunwayBrush => GetBrush(DefaultRunway);
        public static SolidColorBrush CraneBrush => GetBrush(DefaultCrane);
        public static SolidColorBrush JibBrush => GetBrush(DefaultJib);
        public static SolidColorBrush HandoffBrush => GetBrush(HandoffPoint);
        public static SolidColorBrush SelectedBrush => GetBrush(Selected);
    }
}
