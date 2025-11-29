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
