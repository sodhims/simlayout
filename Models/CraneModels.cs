using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Models
{
    #region Runway

    /// <summary>
    /// A runway that EOT cranes travel along
    /// </summary>
    public class RunwayData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Runway";
        private double _startX;
        private double _startY;
        private double _endX;
        private double _endY;
        private double _height = 20;
        private string _color = "#666666";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double StartX
        {
            get => _startX;
            set => SetProperty(ref _startX, value);
        }

        public double StartY
        {
            get => _startY;
            set => SetProperty(ref _startY, value);
        }

        public double EndX
        {
            get => _endX;
            set => SetProperty(ref _endX, value);
        }

        public double EndY
        {
            get => _endY;
            set => SetProperty(ref _endY, value);
        }

        /// <summary>
        /// Height above floor (for info/visualization)
        /// </summary>
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private double _capacity = 5000; // kg
        private string _runwayPairId = ""; // ID of paired parallel runway

        /// <summary>
        /// Capacity in kilograms
        /// </summary>
        public double Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        /// <summary>
        /// ID of the parallel runway paired with this one (for EOT cranes)
        /// </summary>
        public string RunwayPairId
        {
            get => _runwayPairId;
            set => SetProperty(ref _runwayPairId, value);
        }

        [JsonIgnore]
        public double Length => Math.Sqrt(
            Math.Pow(EndX - StartX, 2) +
            Math.Pow(EndY - StartY, 2));

        [JsonIgnore]
        public double Angle => Math.Atan2(EndY - StartY, EndX - StartX) * 180 / Math.PI;

        /// <summary>
        /// Gets position along runway (0.0 = start, 1.0 = end)
        /// </summary>
        public (double x, double y) GetPositionAt(double t)
        {
            t = Math.Clamp(t, 0, 1);
            return (
                StartX + t * (EndX - StartX),
                StartY + t * (EndY - StartY)
            );
        }

        /// <summary>
        /// Gets the normalized parameter t for a point projected onto the runway
        /// </summary>
        public double GetParameterAt(double x, double y)
        {
            var dx = EndX - StartX;
            var dy = EndY - StartY;
            var lenSq = dx * dx + dy * dy;
            if (lenSq < 0.001) return 0;

            var t = ((x - StartX) * dx + (y - StartY) * dy) / lenSq;
            return Math.Clamp(t, 0, 1);
        }

        /// <summary>
        /// Gets perpendicular unit vector (left side when looking from start to end)
        /// </summary>
        public (double x, double y) GetPerpendicular()
        {
            var len = Length;
            if (len < 0.001) return (0, 0);
            return (-(EndY - StartY) / len, (EndX - StartX) / len);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.Infrastructure;
    }

    #endregion

    #region EOT Crane

    /// <summary>
    /// An EOT (Electric Overhead Traveling) crane on a runway
    /// </summary>
    public class EOTCraneData : NotifyBase, IConstrainedEntity
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "EOT";
        private string _runwayId = "";
        private double _reachLeft = 10;
        private double _reachRight = 10;
        private double _zoneMin = 0;
        private double _zoneMax = 1;
        private double _speedBridge = 1.0;
        private double _speedTrolley = 0.5;
        private double _speedHoist = 0.3;
        private string _color = "#E67E22";
        private double _bridgePosition = 0.5; // Current position along runway (0-1)
        private double _bayWidth = 240.0; // Bay width in inches (default 20' = 240")

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string RunwayId
        {
            get => _runwayId;
            set => SetProperty(ref _runwayId, value);
        }

        /// <summary>
        /// Reach perpendicular to runway, left side (looking from start to end)
        /// </summary>
        public double ReachLeft
        {
            get => _reachLeft;
            set => SetProperty(ref _reachLeft, Math.Max(0, value));
        }

        /// <summary>
        /// Reach perpendicular to runway, right side
        /// </summary>
        public double ReachRight
        {
            get => _reachRight;
            set => SetProperty(ref _reachRight, Math.Max(0, value));
        }

        /// <summary>
        /// Zone start as fraction of runway (0 = runway start)
        /// </summary>
        public double ZoneMin
        {
            get => _zoneMin;
            set => SetProperty(ref _zoneMin, Math.Clamp(value, 0, 1));
        }

        /// <summary>
        /// Zone end as fraction of runway (1 = runway end)
        /// </summary>
        public double ZoneMax
        {
            get => _zoneMax;
            set => SetProperty(ref _zoneMax, Math.Clamp(value, 0, 1));
        }

        /// <summary>
        /// Bridge speed along runway (m/s)
        /// </summary>
        public double SpeedBridge
        {
            get => _speedBridge;
            set => SetProperty(ref _speedBridge, Math.Max(0.01, value));
        }

        /// <summary>
        /// Trolley speed perpendicular to runway (m/s)
        /// </summary>
        public double SpeedTrolley
        {
            get => _speedTrolley;
            set => SetProperty(ref _speedTrolley, Math.Max(0.01, value));
        }

        /// <summary>
        /// Hoist speed vertical (m/s)
        /// </summary>
        public double SpeedHoist
        {
            get => _speedHoist;
            set => SetProperty(ref _speedHoist, Math.Max(0.01, value));
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Current bridge position along runway (0 = runway start, 1 = runway end)
        /// Constrained to stay within ZoneMin and ZoneMax
        /// </summary>
        public double BridgePosition
        {
            get => _bridgePosition;
            set => SetProperty(ref _bridgePosition, Math.Clamp(value, ZoneMin, ZoneMax));
        }

        [JsonIgnore]
        public double TotalReach => ReachLeft + ReachRight;

        /// <summary>
        /// Stores the starting position for animation (not serialized)
        /// </summary>
        [JsonIgnore]
        public double AnimationStartPosition { get; set; }

        /// <summary>
        /// Bay width in inches (defines the width of the EOT service bay, default 20' = 240")
        /// This sets ReachLeft and ReachRight symmetrically.
        /// </summary>
        public double BayWidth
        {
            get => _bayWidth;
            set
            {
                if (SetProperty(ref _bayWidth, Math.Max(60, value))) // Minimum 5' bay
                {
                    // Update reaches to be symmetric around runway
                    var halfBay = _bayWidth / 2;
                    _reachLeft = halfBay;
                    _reachRight = halfBay;
                    OnPropertyChanged(nameof(ReachLeft));
                    OnPropertyChanged(nameof(ReachRight));
                    OnPropertyChanged(nameof(TotalReach));
                }
            }
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.OverheadTransport;

        /// <summary>
        /// Gets the coverage polygon for this crane (requires runway)
        /// Returns list of corner points defining the reachable area
        /// </summary>
        public System.Collections.Generic.List<PointData> GetCoveragePolygon(RunwayData runway)
        {
            if (runway == null)
                return new System.Collections.Generic.List<PointData>();

            var corners = CraneZoneCalculator.GetEnvelopePolygon(this, runway);
            var polygon = new System.Collections.Generic.List<PointData>();

            foreach (var (x, y) in corners)
            {
                polygon.Add(new PointData(x, y));
            }

            return polygon;
        }

        /// <summary>
        /// Calculates travel time from one point to another (seconds)
        /// </summary>
        public double CalculateTravelTime(
            double fromBridgePos, double fromTrolleyPos,
            double toBridgePos, double toTrolleyPos,
            double bridgeDistance,
            bool includeHoist = false, double hoistDistance = 0)
        {
            var bridgeTime = Math.Abs(toBridgePos - fromBridgePos) * bridgeDistance / SpeedBridge;
            var trolleyTime = Math.Abs(toTrolleyPos - fromTrolleyPos) / SpeedTrolley;
            var hoistTime = includeHoist ? Math.Abs(hoistDistance) / SpeedHoist : 0;

            // Bridge and trolley move simultaneously, hoist is sequential
            return Math.Max(bridgeTime, trolleyTime) + hoistTime * 2;
        }

        // IConstrainedEntity implementation
        public bool SupportsConstrainedMovement => true;

        public IConstraint GetConstraint()
        {
            // EOT crane constraint is managed by ConstraintFactory
            // which needs the runway data - return null here
            return null;
        }

        /// <summary>
        /// Creates linear constraint for this crane given its runway
        /// </summary>
        public IConstraint GetConstraint(RunwayData runway)
        {
            if (runway == null)
                return null;

            var (startX, startY) = runway.GetPositionAt(ZoneMin);
            var (endX, endY) = runway.GetPositionAt(ZoneMax);

            return new LinearConstraint(
                new Point(startX, startY),
                new Point(endX, endY)
            );
        }
    }

    #endregion

    #region Handoff Point

    /// <summary>
    /// Handoff point types
    /// </summary>
    public static class HandoffTypes
    {
        public const string Direct = "direct";
        public const string GroundBuffer = "ground_buffer";
    }

    /// <summary>
    /// Handoff point between two EOT cranes on the same runway
    /// </summary>
    public class HandoffPointData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Handoff";
        private double _x;
        private double _y;
        private string _runwayId = "";
        private string _crane1Id = "";
        private string _crane2Id = "";
        private double _position;
        private string _handoffType = HandoffTypes.Direct;
        private string _handoffRule = "transfer";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public string RunwayId
        {
            get => _runwayId;
            set => SetProperty(ref _runwayId, value);
        }

        public string Crane1Id
        {
            get => _crane1Id;
            set => SetProperty(ref _crane1Id, value);
        }

        public string Crane2Id
        {
            get => _crane2Id;
            set => SetProperty(ref _crane2Id, value);
        }

        /// <summary>
        /// Position along runway (0-1)
        /// </summary>
        public double Position
        {
            get => _position;
            set => SetProperty(ref _position, Math.Clamp(value, 0, 1));
        }

        /// <summary>
        /// Handoff type: Direct or GroundBuffer
        /// </summary>
        public string HandoffType
        {
            get => _handoffType;
            set => SetProperty(ref _handoffType, value);
        }

        /// <summary>
        /// Handoff rule: "transfer" (direct) or "clearAndPickup" (place and leave)
        /// </summary>
        public string HandoffRule
        {
            get => _handoffRule;
            set => SetProperty(ref _handoffRule, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.OverheadTransport;
    }

    #endregion

    #region Jib Crane

    /// <summary>
    /// A jib crane with fixed pivot point and arc coverage
    /// </summary>
    public class JibCraneData : NotifyBase, IConstrainedEntity
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Jib";
        private double _centerX;
        private double _centerY;
        private double _radius = 20;
        private double _arcStart = 0;
        private double _arcEnd = 360;
        private double _speedSlew = 10;
        private double _speedHoist = 0.3;
        private string _color = "#27AE60";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double CenterX
        {
            get => _centerX;
            set => SetProperty(ref _centerX, value);
        }

        public double CenterY
        {
            get => _centerY;
            set => SetProperty(ref _centerY, value);
        }

        /// <summary>
        /// Reach radius from pivot
        /// </summary>
        public double Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, Math.Max(1, value));
        }

        /// <summary>
        /// Arc start angle (degrees, 0 = right, counterclockwise)
        /// </summary>
        public double ArcStart
        {
            get => _arcStart;
            set => SetProperty(ref _arcStart, value % 360);
        }

        /// <summary>
        /// Arc end angle (degrees)
        /// </summary>
        public double ArcEnd
        {
            get => _arcEnd;
            set => SetProperty(ref _arcEnd, value % 360);
        }

        /// <summary>
        /// Slew speed (degrees/second)
        /// </summary>
        public double SpeedSlew
        {
            get => _speedSlew;
            set => SetProperty(ref _speedSlew, Math.Max(0.1, value));
        }

        /// <summary>
        /// Hoist speed (m/s)
        /// </summary>
        public double SpeedHoist
        {
            get => _speedHoist;
            set => SetProperty(ref _speedHoist, Math.Max(0.01, value));
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        [JsonIgnore]
        public double ArcAngle
        {
            get
            {
                var angle = ArcEnd - ArcStart;
                if (angle < 0) angle += 360;
                return angle;
            }
        }

        [JsonIgnore]
        public bool IsFullCircle => Math.Abs(ArcAngle - 360) < 0.1;

        /// <summary>
        /// Current angle of the jib arm (degrees) - for animation
        /// </summary>
        [JsonIgnore]
        public double CurrentAngle { get; set; }

        /// <summary>
        /// Stores the starting angle for animation (not serialized)
        /// </summary>
        [JsonIgnore]
        public double AnimationStartAngle { get; set; }

        /// <summary>
        /// Animation speed multiplier (uses SpeedSlew internally)
        /// </summary>
        [JsonIgnore]
        public double Speed => SpeedSlew / 10.0; // Normalize to reasonable animation speed

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.OverheadTransport;

        /// <summary>
        /// Gets the coverage polygon for this jib crane
        /// Returns arc/sector approximated as polygon with segments
        /// </summary>
        public System.Collections.Generic.List<PointData> GetCoveragePolygon(int segments = 32)
        {
            var polygon = new System.Collections.Generic.List<PointData>();

            // Add center point
            polygon.Add(new PointData(CenterX, CenterY));

            // Calculate the arc span
            var startRad = ArcStart * Math.PI / 180;
            var endRad = ArcEnd * Math.PI / 180;

            // Handle wrap-around case (e.g., 270° to 90° goes through 0°)
            var arcSpan = ArcAngle;
            if (IsFullCircle)
            {
                // Full circle: create segments from 0 to 360
                for (int i = 0; i <= segments; i++)
                {
                    var angle = (i * 360.0 / segments) * Math.PI / 180;
                    var x = CenterX + Radius * Math.Cos(angle);
                    var y = CenterY + Radius * Math.Sin(angle);
                    polygon.Add(new PointData(x, y));
                }
            }
            else
            {
                // Partial arc: create sector
                for (int i = 0; i <= segments; i++)
                {
                    var t = i / (double)segments;
                    var angle = startRad + t * (arcSpan * Math.PI / 180);
                    var x = CenterX + Radius * Math.Cos(angle);
                    var y = CenterY + Radius * Math.Sin(angle);
                    polygon.Add(new PointData(x, y));
                }
                // Close the sector back to center
                polygon.Add(new PointData(CenterX, CenterY));
            }

            return polygon;
        }

        /// <summary>
        /// Check if a point is within the jib's service area
        /// </summary>
        public bool ContainsPoint(double x, double y)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > Radius) return false;
            if (IsFullCircle) return true;

            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            if (angle < 0) angle += 360;

            if (ArcStart <= ArcEnd)
                return angle >= ArcStart && angle <= ArcEnd;
            else
                return angle >= ArcStart || angle <= ArcEnd;
        }

        // IConstrainedEntity implementation
        public bool SupportsConstrainedMovement => true;

        public IConstraint GetConstraint()
        {
            // Convert angles from degrees to radians
            var startRad = ArcStart * Math.PI / 180;
            var endRad = ArcEnd * Math.PI / 180;

            // Handle wrap-around (e.g., 270° to 90°)
            if (endRad < startRad)
                endRad += 2 * Math.PI;

            return new ArcConstraint(
                new Point(CenterX, CenterY),
                Radius,
                startRad,
                endRad
            );
        }
    }

    #endregion

    #region Drop Zone

    /// <summary>
    /// Drop zone under a crane where loads may be suspended
    /// </summary>
    public class DropZoneData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "DropZone";
        private System.Collections.Generic.List<PointData> _boundary = new();
        private string _craneId = "";
        private bool _isPedestrianExclusion = true;
        private string _color = "#FFA500";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Polygon defining the drop zone area
        /// </summary>
        public System.Collections.Generic.List<PointData> Boundary
        {
            get => _boundary;
            set => SetProperty(ref _boundary, value);
        }

        /// <summary>
        /// ID of the crane that owns this drop zone
        /// </summary>
        public string CraneId
        {
            get => _craneId;
            set => SetProperty(ref _craneId, value);
        }

        /// <summary>
        /// Whether pedestrians should be excluded from this zone
        /// (feeds into pedestrian layer for safety analysis)
        /// </summary>
        public bool IsPedestrianExclusion
        {
            get => _isPedestrianExclusion;
            set => SetProperty(ref _isPedestrianExclusion, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public LayerType ArchitectureLayer => LayerType.OverheadTransport;
    }

    #endregion

    #region Crane Utilities

    /// <summary>
    /// Utility for crane zone calculations
    /// </summary>
    public static class CraneZoneCalculator
    {
        /// <summary>
        /// Finds overlap zone between two cranes on the same runway
        /// </summary>
        public static (double min, double max)? FindOverlap(EOTCraneData crane1, EOTCraneData crane2)
        {
            if (crane1.RunwayId != crane2.RunwayId)
                return null;

            var overlapMin = Math.Max(crane1.ZoneMin, crane2.ZoneMin);
            var overlapMax = Math.Min(crane1.ZoneMax, crane2.ZoneMax);

            if (overlapMin >= overlapMax)
                return null;

            return (overlapMin, overlapMax);
        }

        /// <summary>
        /// Checks if a point is within a crane's service envelope
        /// </summary>
        public static bool IsPointInEnvelope(EOTCraneData crane, RunwayData runway, double x, double y)
        {
            var t = runway.GetParameterAt(x, y);

            if (t < crane.ZoneMin || t > crane.ZoneMax)
                return false;

            var (perpX, perpY) = runway.GetPerpendicular();
            var (runwayX, runwayY) = runway.GetPositionAt(t);

            var perpDist = (x - runwayX) * perpX + (y - runwayY) * perpY;

            if (perpDist > 0 && perpDist > crane.ReachLeft)
                return false;
            if (perpDist < 0 && Math.Abs(perpDist) > crane.ReachRight)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the service envelope polygon corners for a crane
        /// </summary>
        public static (double x, double y)[] GetEnvelopePolygon(EOTCraneData crane, RunwayData runway)
        {
            var (perpX, perpY) = runway.GetPerpendicular();
            if (perpX == 0 && perpY == 0)
                return Array.Empty<(double, double)>();

            var (startX, startY) = runway.GetPositionAt(crane.ZoneMin);
            var (endX, endY) = runway.GetPositionAt(crane.ZoneMax);

            return new[]
            {
                (startX + perpX * crane.ReachLeft, startY + perpY * crane.ReachLeft),
                (endX + perpX * crane.ReachLeft, endY + perpY * crane.ReachLeft),
                (endX - perpX * crane.ReachRight, endY - perpY * crane.ReachRight),
                (startX - perpX * crane.ReachRight, startY - perpY * crane.ReachRight)
            };
        }
    }

    #endregion
}
