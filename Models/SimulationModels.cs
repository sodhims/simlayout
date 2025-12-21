using System.Collections.Generic;

namespace LayoutEditor.Models
{
    /// <summary>
    /// Simulation parameters for nodes
    /// </summary>
    public class SimulationParams : NotifyBase
    {
        private int _servers = 1;
        private int _capacity = 1;
        private int _initialLevel;
        private int _priority = 1;
        private DistributionData? _processTime;
        private DistributionData? _setupTime;
        private DistributionData? _interarrivalTime;
        private double? _mtbf;
        private double? _mttr;
        private string _queueDiscipline = "FIFO";
        private string _blockingMode = "block_upstream";
        private string _entityType = "part";
        private int _batchSize = 1;
        private int? _maxArrivals;
        private bool _collectStatistics = true;
        private Dictionary<string, object> _customProperties = new();

        public int Servers
        {
            get => _servers;
            set => SetProperty(ref _servers, value);
        }

        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        public int InitialLevel
        {
            get => _initialLevel;
            set => SetProperty(ref _initialLevel, value);
        }

        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public DistributionData? ProcessTime
        {
            get => _processTime;
            set => SetProperty(ref _processTime, value);
        }

        public DistributionData? SetupTime
        {
            get => _setupTime;
            set => SetProperty(ref _setupTime, value);
        }

        public DistributionData? InterarrivalTime
        {
            get => _interarrivalTime;
            set => SetProperty(ref _interarrivalTime, value);
        }

        public double? Mtbf
        {
            get => _mtbf;
            set => SetProperty(ref _mtbf, value);
        }

        public double? Mttr
        {
            get => _mttr;
            set => SetProperty(ref _mttr, value);
        }

        public string QueueDiscipline
        {
            get => _queueDiscipline;
            set => SetProperty(ref _queueDiscipline, value);
        }

        public string BlockingMode
        {
            get => _blockingMode;
            set => SetProperty(ref _blockingMode, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, value);
        }

        public int? MaxArrivals
        {
            get => _maxArrivals;
            set => SetProperty(ref _maxArrivals, value);
        }

        public bool CollectStatistics
        {
            get => _collectStatistics;
            set => SetProperty(ref _collectStatistics, value);
        }

        public Dictionary<string, object> CustomProperties
        {
            get => _customProperties;
            set => SetProperty(ref _customProperties, value);
        }
    }

    /// <summary>
    /// Statistical distribution data
    /// </summary>
    public class DistributionData : NotifyBase
    {
        private string _distribution = "constant";
        private double _value;
        private double _mean;
        private double _stdDev;
        private double _min;
        private double _max;
        private double _mode;
        private double _shape;
        private double _scale = 1.0;
        private string _unit = "minutes";

        public string Distribution
        {
            get => _distribution;
            set => SetProperty(ref _distribution, value);
        }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public double Mean
        {
            get => _mean;
            set => SetProperty(ref _mean, value);
        }

        public double StdDev
        {
            get => _stdDev;
            set => SetProperty(ref _stdDev, value);
        }

        public double Min
        {
            get => _min;
            set => SetProperty(ref _min, value);
        }

        public double Max
        {
            get => _max;
            set => SetProperty(ref _max, value);
        }

        public double Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public double Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }

        public double Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public override string ToString()
        {
            return Distribution switch
            {
                "constant" => $"Const({Value})",
                "exponential" => $"Exp(μ={Mean})",
                "normal" => $"N(μ={Mean}, σ={StdDev})",
                "uniform" => $"U({Min}, {Max})",
                "triangular" => $"Tri({Min}, {Mode}, {Max})",
                "weibull" => $"Weibull(k={Shape}, λ={Scale})",
                _ => Distribution
            };
        }
    }
}
