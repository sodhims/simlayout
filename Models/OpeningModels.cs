using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    #region Opening States and Types

    /// <summary>
    /// Opening state - all openings have a state machine
    /// </summary>
    public static class OpeningStates
    {
        public const string Open = "open";
        public const string Closed = "closed";
        public const string Locked = "locked";
        public const string Emergency = "emergency"; // Always open, cannot be closed
    }

    /// <summary>
    /// Opening types for categorization
    /// </summary>
    public static class OpeningTypes
    {
        // Unconstrained (capacity = 0)
        public const string Aisle = "aisle";
        public const string BayEntrance = "bay_entrance";
        public const string EmergencyExit = "emergency_exit";

        // Constrained (capacity >= 1)
        public const string Door = "door";
        public const string Hatch = "hatch";
        public const string Manhole = "manhole";
        public const string Gate = "gate";
    }

    /// <summary>
    /// Direction constraints for openings
    /// </summary>
    public static class DirectionModes
    {
        public const string Bidirectional = "bidirectional";
        public const string InboundOnly = "inbound_only";
        public const string OutboundOnly = "outbound_only";
    }

    #endregion

    #region Opening Base Model

    /// <summary>
    /// Base class for all openings (doors, hatches, aisles, gates, etc.)
    /// Openings connect zones or provide passage through walls.
    /// </summary>
    public class OpeningData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private double _x, _y;
        private double _rotation = 0; // Degrees
        private string _openingType = OpeningTypes.Door;

        // Capacity system: 0 = unconstrained, N = requires token
        private int _capacity = 1;

        // State machine
        private string _state = OpeningStates.Open;

        // Physical constraints
        private double _clearWidth = 36; // inches
        private double _clearHeight = 80; // inches
        private double _maxLoadWeight = 0; // kg, 0 = no limit

        // Entity filtering
        private List<string> _allowedEntityTypes = null!; // null = all allowed

        // Directional flow
        private string _directionMode = DirectionModes.Bidirectional;

        // Traversal time (can be extended to distribution later)
        private double _traversalTime = 2.0; // seconds

        // Zone connections
        private string _fromZoneId = "";
        private string _toZoneId = "";

        // Blocking conditions
        private List<string> _blockingConditions = new();

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

        public double Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        public string OpeningType
        {
            get => _openingType;
            set => SetProperty(ref _openingType, value);
        }

        /// <summary>
        /// Capacity: 0 = unconstrained (no token required), N = limited (token required)
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, Math.Max(0, value));
        }

        /// <summary>
        /// Current state: Open, Closed, Locked, Emergency
        /// </summary>
        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public double ClearWidth
        {
            get => _clearWidth;
            set => SetProperty(ref _clearWidth, value);
        }

        public double ClearHeight
        {
            get => _clearHeight;
            set => SetProperty(ref _clearHeight, value);
        }

        public double MaxLoadWeight
        {
            get => _maxLoadWeight;
            set => SetProperty(ref _maxLoadWeight, value);
        }

        public List<string> AllowedEntityTypes
        {
            get => _allowedEntityTypes;
            set => SetProperty(ref _allowedEntityTypes, value);
        }

        public string DirectionMode
        {
            get => _directionMode;
            set => SetProperty(ref _directionMode, value);
        }

        public double TraversalTime
        {
            get => _traversalTime;
            set => SetProperty(ref _traversalTime, value);
        }

        public string FromZoneId
        {
            get => _fromZoneId;
            set => SetProperty(ref _fromZoneId, value);
        }

        public string ToZoneId
        {
            get => _toZoneId;
            set => SetProperty(ref _toZoneId, value);
        }

        public List<string> BlockingConditions
        {
            get => _blockingConditions;
            set => SetProperty(ref _blockingConditions, value);
        }

        /// <summary>
        /// Check if opening is passable based on current state
        /// </summary>
        [JsonIgnore]
        public bool IsPassable => _state == OpeningStates.Open || _state == OpeningStates.Emergency;

        /// <summary>
        /// Check if this is an unconstrained opening (no capacity limit)
        /// </summary>
        [JsonIgnore]
        public bool IsUnconstrained => _capacity == 0;

        /// <summary>
        /// Transport layer assignment for 8-layer architecture
        /// </summary>
        [JsonIgnore]
        public virtual LayerType ArchitectureLayer => LayerType.Infrastructure;
    }

    #endregion

    #region Unconstrained Opening (Capacity = 0)

    /// <summary>
    /// Unconstrained openings - no capacity limit, no tokens required
    /// Examples: Aisles, bay entrances, emergency exits
    /// </summary>
    public class UnconstrainedOpening : OpeningData
    {
        public UnconstrainedOpening()
        {
            Capacity = 0; // Force capacity to 0
        }

        private double _width = 120; // Wider than constrained openings

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
    }

    #endregion

    #region Constrained Opening Base (Capacity >= 1)

    /// <summary>
    /// Base class for constrained openings (doors, hatches, manholes, gates)
    /// Requires token for traversal
    /// </summary>
    public class ConstrainedOpening : OpeningData
    {
        public ConstrainedOpening()
        {
            Capacity = 1; // Default capacity
        }

        private double _width = 36;
        private double _height = 80;

        public double Width
        {
            get => _width;
            set
            {
                SetProperty(ref _width, value);
                ClearWidth = value; // Keep ClearWidth in sync
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                SetProperty(ref _height, value);
                ClearHeight = value; // Keep ClearHeight in sync
            }
        }
    }

    #endregion

    #region Door Subtype

    /// <summary>
    /// Door - standard hinged or sliding door
    /// </summary>
    public class DoorOpening : ConstrainedOpening
    {
        private string _swingDirection = SwingDirections.Inward;
        private double _swingAngle = 90; // Degrees
        private bool _autoClose = false;
        private string _accessControl = ""; // Access control system ID

        public DoorOpening()
        {
            OpeningType = OpeningTypes.Door;
        }

        public string SwingDirection
        {
            get => _swingDirection;
            set => SetProperty(ref _swingDirection, value);
        }

        public double SwingAngle
        {
            get => _swingAngle;
            set => SetProperty(ref _swingAngle, value);
        }

        public bool AutoClose
        {
            get => _autoClose;
            set => SetProperty(ref _autoClose, value);
        }

        public string AccessControl
        {
            get => _accessControl;
            set => SetProperty(ref _accessControl, value);
        }
    }

    public static class SwingDirections
    {
        public const string Inward = "inward";
        public const string Outward = "outward";
        public const string Sliding = "sliding";
        public const string Bidirectional = "bidirectional";
    }

    #endregion

    #region Hatch Subtype

    /// <summary>
    /// Hatch - vertical or horizontal access opening with ladder
    /// </summary>
    public class HatchOpening : ConstrainedOpening
    {
        private bool _isVertical = true;
        private double _ladderTime = 5.0; // Additional time to traverse ladder (seconds)

        public HatchOpening()
        {
            OpeningType = OpeningTypes.Hatch;
            Width = 24;
            Height = 24;
        }

        public bool IsVertical
        {
            get => _isVertical;
            set => SetProperty(ref _isVertical, value);
        }

        public double LadderTime
        {
            get => _ladderTime;
            set => SetProperty(ref _ladderTime, value);
        }
    }

    #endregion

    #region Manhole Subtype

    /// <summary>
    /// Manhole - confined space access
    /// </summary>
    public class ManholeOpening : ConstrainedOpening
    {
        private bool _confinedSpaceProtocol = true;
        private bool _requiresPermit = true;

        public ManholeOpening()
        {
            OpeningType = OpeningTypes.Manhole;
            Width = 24;
            Height = 24;
            Capacity = 1; // Only one person at a time
        }

        public bool ConfinedSpaceProtocol
        {
            get => _confinedSpaceProtocol;
            set => SetProperty(ref _confinedSpaceProtocol, value);
        }

        public bool RequiresPermit
        {
            get => _requiresPermit;
            set => SetProperty(ref _requiresPermit, value);
        }
    }

    #endregion

    #region Gate Subtype

    /// <summary>
    /// Gate - vehicle or large equipment passage
    /// </summary>
    public class GateOpening : ConstrainedOpening
    {
        private bool _vehicleOnly = false;
        private string _barrierType = ""; // "sliding", "swing", "lift", etc.

        public GateOpening()
        {
            OpeningType = OpeningTypes.Gate;
            Width = 144; // 12 feet
            Height = 120; // 10 feet
        }

        public bool VehicleOnly
        {
            get => _vehicleOnly;
            set => SetProperty(ref _vehicleOnly, value);
        }

        public string BarrierType
        {
            get => _barrierType;
            set => SetProperty(ref _barrierType, value);
        }
    }

    #endregion

    #region Temporary Opening Extension

    /// <summary>
    /// Temporary opening - exists only during certain assembly states (for shipbuilding)
    /// </summary>
    public class TemporaryOpening : OpeningData
    {
        private string _existsFromState = "";
        private string _existsUntilState = "";
        private string _createdByOperationId = "";

        public string ExistsFromState
        {
            get => _existsFromState;
            set => SetProperty(ref _existsFromState, value);
        }

        public string ExistsUntilState
        {
            get => _existsUntilState;
            set => SetProperty(ref _existsUntilState, value);
        }

        public string CreatedByOperationId
        {
            get => _createdByOperationId;
            set => SetProperty(ref _createdByOperationId, value);
        }

        /// <summary>
        /// Check if opening exists in given state
        /// </summary>
        public bool ExistsInState(string currentState)
        {
            if (string.IsNullOrEmpty(_existsFromState))
                return true; // Permanent opening

            // Simple string comparison - can be enhanced with state ordering
            return currentState == _existsFromState ||
                   (!string.IsNullOrEmpty(_existsUntilState) && currentState != _existsUntilState);
        }
    }

    #endregion
}
