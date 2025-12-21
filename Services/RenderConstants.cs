namespace LayoutEditor.Services
{
    /// <summary>
    /// Centralized constants for rendering - single source of truth for sizes, positions, etc.
    /// </summary>
    public static class RenderConstants
    {
        // Node dimensions
        public const double NodeWidth = 50;
        public const double NodeHeight = 60;
        public const double NodeCornerRadius = 4;
        
        // Node terminals (increased for easier clicking)
        public const double NodeTerminalRadius = 8;      // Was 5 - now 16px diameter visual
        public const double NodeTerminalStickOut = 12;   // Was 10 - slightly longer stem
        public const double NodeTerminalStemWidth = 2.5; // Was 2 - slightly thicker stem
        
        // Cell terminals (larger for cells)
        public const double CellTerminalRadius = 10;     // Was 8
        public const double CellTerminalStickOut = 14;   // Was 12
        public const double CellTerminalStemWidth = 3;
        
        // Terminal positions - calculated from node dimensions
        public static double GetNodeCenterY(double nodeHeight) => nodeHeight / 2.0;
        
        // Icon dimensions
        public const double IconSize = 28;
        public const double IconTopMargin = 4;
        
        // Hit testing (generous hit areas for easy clicking)
        public const double TerminalHitRadius = 14;      // Was 10 - now 28px diameter hit area
        public const double NodeHitMargin = 8;
        public const double PathHitMargin = 8;
        public const double GroupBorderThickness = 12;
        
        // Path rendering
        public const double PathArrowSize = 8;
        public const double PathDefaultWidth = 2;
        
        // Group/Cell padding
        public const double GroupPadding = 15;
        
        // Wall defaults
        public const double WallThicknessExterior = 10;
        public const double WallThicknessStandard = 6;
        public const double WallThicknessPartition = 4;
        public const double WallThicknessGlass = 3;
        
        // Grid
        public const double DefaultGridSize = 20;
        public const double MinGridSize = 5;
        public const double MaxGridSize = 100;
        
        // Selection
        public const double SelectionDashLength = 4;
        public const double SelectionDashGap = 2;
        
        // Badge sizes
        public const double BadgeCornerRadius = 6;
        public const double BadgeFontSize = 7;
    }
}
