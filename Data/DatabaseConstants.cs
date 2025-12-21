namespace LayoutEditor.Data
{
    /// <summary>
    /// Database element type constants (matches ElementType column in Elements table)
    /// </summary>
    public static class DbElementType
    {
        // Infrastructure Layer (Layer 0)
        public const string Wall = "Wall";
        public const string Opening = "Opening";
        public const string Column = "Column";

        // AGV Layer (Layer 1)
        public const string AGVPath = "AGVPath";
        public const string Node = "Node";

        // Conveyor Layer (Layer 2)
        public const string Conveyor = "Conveyor";

        // Crane Layer (Layer 3)
        public const string EOTCrane = "EOTCrane";
        public const string PrimaryAisle = "PrimaryAisle";
        public const string SecondaryAisle = "SecondaryAisle";
        public const string CraneBlock = "CraneBlock";

        // Overhead Transport Layer (Layer 4)
        public const string CraneCoverageZone = "CraneCoverageZone";
        public const string HandoffPoint = "HandoffPoint";
        public const string DropZone = "DropZone";

        // Flexible Transport Layer (Layer 5)
        public const string ForkliftAisle = "ForkliftAisle";
        public const string StagingArea = "StagingArea";
        public const string CrossingZone = "CrossingZone";

        // Pedestrian Layer (Layer 7)
        public const string Walkway = "Walkway";
        public const string PedestrianCrossing = "PedestrianCrossing";
        public const string SafetyZone = "SafetyZone";

        // Conflict Management (Layer 8)
        public const string ConflictResolution = "ConflictResolution";
        public const string LayerConnection = "LayerConnection";

        // Annotation Layer (Layer 7)
        public const string AreaMarker = "AreaMarker";
        public const string TextLabel = "TextLabel";
    }

    /// <summary>
    /// Database connection type constants (matches ConnectionType column in Connections table)
    /// </summary>
    public static class DbConnectionType
    {
        // Crane-to-Crane handoffs
        public const string CraneHandoff = "CraneHandoff";

        // Crane-to-Conveyor drop zones
        public const string CraneDropZone = "CraneDropZone";

        // AGV-Forklift crossings
        public const string AGVForkliftCrossing = "AGVForkliftCrossing";

        // Generic connection
        public const string Generic = "Generic";
    }

    /// <summary>
    /// Database zone type constants (matches ZoneType column in Zones table)
    /// </summary>
    public static class DbZoneType
    {
        // Functional zones
        public const string Functional = "Functional";

        // Safety zones
        public const string Safety = "Safety";

        // Custom user-defined zones
        public const string Custom = "Custom";

        // Operational zones
        public const string Operational = "Operational";
    }

    /// <summary>
    /// Database query constants
    /// </summary>
    public static class DbQuery
    {
        // Date format for SQLite (ISO 8601)
        public const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        // Default layout values
        public const string DefaultUnit = "meters";
        public const double DefaultWidth = 100.0;
        public const double DefaultHeight = 100.0;

        // Pagination
        public const int DefaultPageSize = 100;
        public const int MaxPageSize = 1000;
    }

    /// <summary>
    /// Database error messages
    /// </summary>
    public static class DbErrorMessages
    {
        public const string LayoutNotFound = "Layout not found";
        public const string ElementNotFound = "Element not found";
        public const string ConnectionNotFound = "Connection not found";
        public const string ZoneNotFound = "Zone not found";
        public const string InvalidElementType = "Invalid element type";
        public const string InvalidConnectionType = "Invalid connection type";
        public const string InvalidZoneType = "Invalid zone type";
        public const string DatabaseNotInitialized = "Database not initialized";
        public const string MigrationFailed = "Database migration failed";
        public const string SerializationFailed = "Failed to serialize element properties";
        public const string DeserializationFailed = "Failed to deserialize element properties";
    }
}
