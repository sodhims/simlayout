using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Group of nodes (can be a work cell)
    /// </summary>
    public class GroupData : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _color = "#9B59B6";
        private bool _collapsed;
        private bool _isCell;
        private string _inputTerminalPosition = "left";
        private string _outputTerminalPosition = "right";
        private string _cellType = CellPatterns.Simple;
        private int _cellIndex = 0;

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

        public string CellType
        {
            get => _cellType;
            set => SetProperty(ref _cellType, value);
        }

        public int CellIndex
        {
            get => _cellIndex;
            set => SetProperty(ref _cellIndex, value);
        }

        [JsonIgnore]
        public string CanonicalName => $"C{CellIndex}";

        public List<string> Members { get; set; } = new();
        public List<string> InternalPaths { get; set; } = new();

        public Dictionary<string, List<InternalRoute>> InternalRouting { get; set; } = new();

        public AssemblyRule? AssemblyRule { get; set; }

        public List<string> EntryPoints { get; set; } = new();
        public List<string> ExitPoints { get; set; } = new();

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public bool Collapsed
        {
            get => _collapsed;
            set => SetProperty(ref _collapsed, value);
        }

        public bool IsCell
        {
            get => _isCell;
            set => SetProperty(ref _isCell, value);
        }

        public string InputTerminalPosition
        {
            get => _inputTerminalPosition;
            set => SetProperty(ref _inputTerminalPosition, value);
        }

        public string OutputTerminalPosition
        {
            get => _outputTerminalPosition;
            set => SetProperty(ref _outputTerminalPosition, value);
        }

        /// <summary>
        /// Combined terminal layout (convenience property)
        /// </summary>
        [JsonIgnore]
        public string TerminalLayout
        {
            get => TerminalLayouts.BuildLayout(_inputTerminalPosition, _outputTerminalPosition);
            set
            {
                var (input, output) = TerminalLayouts.ParseLayout(value);
                if (_inputTerminalPosition != input || _outputTerminalPosition != output)
                {
                    _inputTerminalPosition = input;
                    _outputTerminalPosition = output;
                    OnPropertyChanged(nameof(InputTerminalPosition));
                    OnPropertyChanged(nameof(OutputTerminalPosition));
                    OnPropertyChanged(nameof(TerminalLayout));
                }
            }
        }

        /// <summary>
        /// Rotate cell terminal pair (Ctrl+F on cell)
        /// </summary>
        public void FlipTerminals()
        {
            TerminalLayout = TerminalLayouts.GetNextLayout(TerminalLayout);
        }

        /// <summary>
        /// Toggle to 90° mode, then cycle (Ctrl+Shift+F on cell)
        /// </summary>
        public void RotateTerminals90()
        {
            TerminalLayout = TerminalLayouts.Toggle90Degrees(TerminalLayout);
        }

        /// <summary>
        /// Flip cell contents horizontally (mirror left-right)
        /// Also swaps left/right terminal positions
        /// </summary>
        public void FlipHorizontal(IEnumerable<NodeData> allNodes)
        {
            var memberNodes = allNodes.Where(n => Members.Contains(n.Id)).ToList();
            if (!memberNodes.Any()) return;

            // Find bounding box
            double minX = memberNodes.Min(n => n.Visual.X);
            double maxX = memberNodes.Max(n => n.Visual.X + n.Visual.Width);
            double centerX = (minX + maxX) / 2;

            // Mirror each node's X position around center
            foreach (var node in memberNodes)
            {
                double nodeCenter = node.Visual.X + node.Visual.Width / 2;
                double distFromCenter = nodeCenter - centerX;
                node.Visual.X = centerX - distFromCenter - node.Visual.Width / 2;

                // Swap node terminal positions left<->right
                var inputPos = node.Visual.InputTerminalPosition;
                var outputPos = node.Visual.OutputTerminalPosition;
                node.Visual.InputTerminalPosition = SwapLeftRight(inputPos);
                node.Visual.OutputTerminalPosition = SwapLeftRight(outputPos);
            }

            // Swap cell terminal positions left<->right
            InputTerminalPosition = SwapLeftRight(InputTerminalPosition);
            OutputTerminalPosition = SwapLeftRight(OutputTerminalPosition);

            // Swap entry/exit if they were swapped
            var tempEntry = new List<string>(EntryPoints);
            var tempExit = new List<string>(ExitPoints);
            // Don't swap entry/exit points - they're node IDs, not positions
        }

        /// <summary>
        /// Flip cell contents vertically (mirror top-bottom)
        /// Also swaps top/bottom terminal positions
        /// </summary>
        public void FlipVertical(IEnumerable<NodeData> allNodes)
        {
            var memberNodes = allNodes.Where(n => Members.Contains(n.Id)).ToList();
            if (!memberNodes.Any()) return;

            // Find bounding box
            double minY = memberNodes.Min(n => n.Visual.Y);
            double maxY = memberNodes.Max(n => n.Visual.Y + n.Visual.Height);
            double centerY = (minY + maxY) / 2;

            // Mirror each node's Y position around center
            foreach (var node in memberNodes)
            {
                double nodeCenter = node.Visual.Y + node.Visual.Height / 2;
                double distFromCenter = nodeCenter - centerY;
                node.Visual.Y = centerY - distFromCenter - node.Visual.Height / 2;

                // Swap node terminal positions top<->bottom
                var inputPos = node.Visual.InputTerminalPosition;
                var outputPos = node.Visual.OutputTerminalPosition;
                node.Visual.InputTerminalPosition = SwapTopBottom(inputPos);
                node.Visual.OutputTerminalPosition = SwapTopBottom(outputPos);
            }

            // Swap cell terminal positions top<->bottom
            InputTerminalPosition = SwapTopBottom(InputTerminalPosition);
            OutputTerminalPosition = SwapTopBottom(OutputTerminalPosition);
        }

        /// <summary>
        /// Rotate cell contents 90 degrees clockwise
        /// </summary>
        public void Rotate90Clockwise(IEnumerable<NodeData> allNodes)
        {
            var memberNodes = allNodes.Where(n => Members.Contains(n.Id)).ToList();
            if (!memberNodes.Any()) return;

            // Find bounding box center
            double minX = memberNodes.Min(n => n.Visual.X);
            double maxX = memberNodes.Max(n => n.Visual.X + n.Visual.Width);
            double minY = memberNodes.Min(n => n.Visual.Y);
            double maxY = memberNodes.Max(n => n.Visual.Y + n.Visual.Height);
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            // Rotate each node 90° clockwise around center
            foreach (var node in memberNodes)
            {
                double nodeX = node.Visual.X + node.Visual.Width / 2;
                double nodeY = node.Visual.Y + node.Visual.Height / 2;

                // Translate to origin, rotate, translate back
                double relX = nodeX - centerX;
                double relY = nodeY - centerY;

                // 90° clockwise: (x, y) -> (y, -x)
                double newRelX = relY;
                double newRelY = -relX;

                node.Visual.X = centerX + newRelX - node.Visual.Width / 2;
                node.Visual.Y = centerY + newRelY - node.Visual.Height / 2;

                // Rotate terminal positions
                node.Visual.InputTerminalPosition = Rotate90CW(node.Visual.InputTerminalPosition);
                node.Visual.OutputTerminalPosition = Rotate90CW(node.Visual.OutputTerminalPosition);
            }

            // Rotate cell terminal positions
            InputTerminalPosition = Rotate90CW(InputTerminalPosition);
            OutputTerminalPosition = Rotate90CW(OutputTerminalPosition);
        }

        private static string SwapLeftRight(string position)
        {
            return position?.ToLower() switch
            {
                "left" => "right",
                "right" => "left",
                _ => position ?? "left"
            };
        }

        private static string SwapTopBottom(string position)
        {
            return position?.ToLower() switch
            {
                "top" => "bottom",
                "bottom" => "top",
                _ => position ?? "left"
            };
        }

        private static string Rotate90CW(string position)
        {
            return position?.ToLower() switch
            {
                "left" => "top",
                "top" => "right",
                "right" => "bottom",
                "bottom" => "left",
                _ => position ?? "left"
            };
        }

        public void BuildInternalRouting(IEnumerable<PathData> paths)
        {
            InternalRouting.Clear();

            foreach (var path in paths)
            {
                if (!Members.Contains(path.From) || !Members.Contains(path.To)) continue;

                if (!InternalRouting.ContainsKey(path.From))
                    InternalRouting[path.From] = new List<InternalRoute>();

                int priority = InternalRouting[path.From].Count + 1;

                InternalRouting[path.From].Add(new InternalRoute
                {
                    To = path.To,
                    Priority = priority,
                    Probability = 1.0
                });
            }

            DetectCellType();
        }

        public void DetectCellType()
        {
            if (InternalRouting.Count == 0)
            {
                CellType = CellPatterns.Simple;
                return;
            }

            int entryCount = EntryPoints.Count;
            int exitCount = ExitPoints.Count;

            if (entryCount == 1 && exitCount > 1)
            {
                bool hasProbabilistic = InternalRouting.Values
                    .SelectMany(routes => routes)
                    .Any(r => r.Probability < 1.0 && r.Probability > 0);

                CellType = hasProbabilistic ? CellPatterns.Probabilistic : CellPatterns.Parallel;
                return;
            }

            var targetCounts = InternalRouting.Values
                .SelectMany(routes => routes)
                .GroupBy(r => r.To)
                .Where(g => g.Count() > 1)
                .ToList();

            if (targetCounts.Any() || (entryCount > 1 && exitCount == 1))
            {
                CellType = CellPatterns.Assembly;
                string assemblyMachine = targetCounts.FirstOrDefault()?.Key
                    ?? ExitPoints.FirstOrDefault() ?? "";

                var sources = InternalRouting
                    .Where(kvp => kvp.Value.Any(r => r.To == assemblyMachine))
                    .Select(kvp => kvp.Key)
                    .ToList();

                AssemblyRule = new AssemblyRule { MachineId = assemblyMachine, Mode = AssemblyModes.All };
                foreach (var source in sources.Any() ? sources : EntryPoints)
                    AssemblyRule.Requires.Add(source);
                return;
            }

            if (Members.Count > 2 && InternalRouting.Values.All(routes => routes.Count == 1))
            {
                CellType = CellPatterns.Workcell;
                return;
            }

            CellType = CellPatterns.Simple;
        }

        public string GetCanonicalNodeName(NodeData node, int indexInCell)
        {
            return $"{CanonicalName}{node.TypePrefix}{indexInCell}";
        }
    }

    /// <summary>
    /// Internal routing rule within a cell
    /// </summary>
    public class InternalRoute : NotifyBase
    {
        private string _to = "";
        private int _priority = 1;
        private double _probability = 1.0;

        public string To
        {
            get => _to;
            set => SetProperty(ref _to, value);
        }

        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public double Probability
        {
            get => _probability;
            set => SetProperty(ref _probability, value);
        }
    }

    /// <summary>
    /// Assembly rule for multi-input cells
    /// </summary>
    public class AssemblyRule : NotifyBase
    {
        private string _machineId = "";
        private string _mode = AssemblyModes.All;
        private int _count = 1;

        [JsonPropertyName("machine")]
        public string MachineId
        {
            get => _machineId;
            set => SetProperty(ref _machineId, value);
        }

        public List<string> Requires { get; set; } = new();

        public string Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    /// <summary>
    /// Cell pattern types
    /// </summary>
    public static class CellPatterns
    {
        public const string Simple = "simple";
        public const string Parallel = "parallel";
        public const string Assembly = "assembly";
        public const string Workcell = "workcell";
        public const string Robot = "robot";
        public const string Probabilistic = "probabilistic";
    }

    /// <summary>
    /// Assembly modes
    /// </summary>
    public static class AssemblyModes
    {
        public const string All = "all";
        public const string Any = "any";
        public const string Count = "count";
    }
}
