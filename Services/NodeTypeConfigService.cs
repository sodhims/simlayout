using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Represents a single node type configuration
    /// </summary>
    public class NodeTypeConfig
    {
        public string Key { get; set; } = "";
        public string DisplayName { get; set; } = "Node";
        public string Label { get; set; } = "";
        public string Color { get; set; } = "#4A90D9";
        public string Icon { get; set; } = "cnc_mill";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
        public int DefaultWidth { get; set; } = 80;
        public int DefaultHeight { get; set; } = 60;
    }

    /// <summary>
    /// Root object for the node types configuration file
    /// </summary>
    public class NodeTypesConfiguration
    {
        public string Version { get; set; } = "1.0";
        public DateTime LastModified { get; set; } = DateTime.Now;
        public List<NodeTypeConfig> NodeTypes { get; set; } = new();
    }

    /// <summary>
    /// Service for managing node type configurations
    /// </summary>
    public class NodeTypeConfigService
    {
        private static NodeTypeConfigService? _instance;
        public static NodeTypeConfigService Instance => _instance ??= new NodeTypeConfigService();

        private readonly string _configFilePath;
        private Dictionary<string, NodeTypeConfig> _nodeTypes = new(StringComparer.OrdinalIgnoreCase);
        private NodeTypesConfiguration _configuration = new();

        public event EventHandler? ConfigurationChanged;

        private NodeTypeConfigService()
        {
            try
            {
                _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "NodeTypes.json");
                
                // Ensure Config directory exists
                var configDir = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing NodeTypeConfigService: {ex.Message}");
                CreateDefaultConfiguration();
                RebuildLookup();
            }
        }

        /// <summary>
        /// Get all node types
        /// </summary>
        public IEnumerable<NodeTypeConfig> GetAllNodeTypes() => _configuration.NodeTypes;

        /// <summary>
        /// Get node types by category
        /// </summary>
        public IEnumerable<NodeTypeConfig> GetNodeTypesByCategory(string category) =>
            _configuration.NodeTypes.FindAll(n => 
                string.Equals(n.Category, category, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Get all unique categories
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var nt in _configuration.NodeTypes)
            {
                if (!string.IsNullOrEmpty(nt.Category))
                    categories.Add(nt.Category);
            }
            return categories;
        }

        /// <summary>
        /// Get a specific node type by key
        /// </summary>
        public NodeTypeConfig? GetNodeType(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            return _nodeTypes.TryGetValue(key, out var config) ? config : null;
        }

        /// <summary>
        /// Check if a node type exists
        /// </summary>
        public bool HasNodeType(string key) => 
            !string.IsNullOrEmpty(key) && _nodeTypes.ContainsKey(key);

        /// <summary>
        /// Add or update a node type
        /// </summary>
        public void SaveNodeType(NodeTypeConfig nodeType)
        {
            if (string.IsNullOrEmpty(nodeType.Key))
                throw new ArgumentException("Node type key cannot be empty");

            // Update existing or add new
            var existing = _configuration.NodeTypes.FindIndex(n => 
                string.Equals(n.Key, nodeType.Key, StringComparison.OrdinalIgnoreCase));
            
            if (existing >= 0)
            {
                _configuration.NodeTypes[existing] = nodeType;
            }
            else
            {
                _configuration.NodeTypes.Add(nodeType);
            }

            _nodeTypes[nodeType.Key] = nodeType;
            _configuration.LastModified = DateTime.Now;
            
            SaveConfiguration();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Delete a node type
        /// </summary>
        public bool DeleteNodeType(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var removed = _configuration.NodeTypes.RemoveAll(n => 
                string.Equals(n.Key, key, StringComparison.OrdinalIgnoreCase)) > 0;
            
            if (removed)
            {
                _nodeTypes.Remove(key);
                _configuration.LastModified = DateTime.Now;
                SaveConfiguration();
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
            
            return removed;
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<NodeTypesConfiguration>(json, GetJsonOptions());
                    if (config != null)
                    {
                        _configuration = config;
                    }
                }
                else
                {
                    // Create default configuration
                    CreateDefaultConfiguration();
                    SaveConfiguration();
                }

                // Build lookup dictionary
                RebuildLookup();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading node type config: {ex.Message}");
                CreateDefaultConfiguration();
                RebuildLookup();
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_configuration, GetJsonOptions());
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving node type config: {ex.Message}");
            }
        }

        /// <summary>
        /// Reload configuration from file
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Export configuration to a file
        /// </summary>
        public void ExportConfiguration(string filePath)
        {
            var json = JsonSerializer.Serialize(_configuration, GetJsonOptions());
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Import configuration from a file
        /// </summary>
        public void ImportConfiguration(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<NodeTypesConfiguration>(json, GetJsonOptions());
            if (config != null)
            {
                _configuration = config;
                _configuration.LastModified = DateTime.Now;
                RebuildLookup();
                SaveConfiguration();
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RebuildLookup()
        {
            _nodeTypes.Clear();
            foreach (var nt in _configuration.NodeTypes)
            {
                if (!string.IsNullOrEmpty(nt.Key))
                    _nodeTypes[nt.Key] = nt;
            }
        }

        private JsonSerializerOptions GetJsonOptions() => new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private void CreateDefaultConfiguration()
        {
            _configuration = new NodeTypesConfiguration
            {
                Version = "1.0",
                LastModified = DateTime.Now,
                NodeTypes = new List<NodeTypeConfig>
                {
                    // Sources and Sinks
                    new() { Key = "source", DisplayName = "Source", Label = "IN", Color = "#2ECC71", Icon = "source_arrow", Category = "Flow" },
                    new() { Key = "sink", DisplayName = "Sink", Label = "OUT", Color = "#E74C3C", Icon = "sink_arrow", Category = "Flow" },
                    
                    // Machines and Processing
                    new() { Key = "machine", DisplayName = "Machine", Color = "#3498DB", Icon = "press_hydraulic", Category = "Processing" },
                    new() { Key = "workstation", DisplayName = "Workstation", Color = "#9B59B6", Icon = "workstation_manual", Category = "Processing" },
                    new() { Key = "inspection", DisplayName = "Inspection", Color = "#1ABC9C", Icon = "inspection_visual", Category = "Processing" },
                    new() { Key = "assembly", DisplayName = "Assembly", Color = "#2980B9", Icon = "welder_mig", Category = "Processing" },
                    new() { Key = "robot", DisplayName = "Robot", Color = "#9B59B6", Icon = "robot_scara", Category = "Processing" },
                    new() { Key = "packaging", DisplayName = "Packaging", Color = "#C0392B", Icon = "container", Category = "Processing" },
                    
                    // Buffers and Storage
                    new() { Key = "buffer", DisplayName = "Buffer", Color = "#F5A623", Icon = "buffer_fifo", Category = "Storage" },
                    new() { Key = "storage", DisplayName = "Storage", Color = "#95A5A6", Icon = "shelf_unit", Category = "Storage" },
                    new() { Key = "rack", DisplayName = "Rack", Color = "#795548", Icon = "shelf_unit", Category = "Storage" },
                    new() { Key = "pallet", DisplayName = "Pallet", Color = "#8D6E63", Icon = "pallet", Category = "Storage" },
                    new() { Key = "bin", DisplayName = "Bin", Color = "#A1887F", Icon = "bin", Category = "Storage" },
                    new() { Key = "shelf", DisplayName = "Shelf", Color = "#6D4C41", Icon = "shelf_unit", Category = "Storage" },
                    
                    // Conveyors and Junctions
                    new() { Key = "conveyor", DisplayName = "Conveyor", Color = "#7F8C8D", Icon = "conveyor_belt", Category = "Transport" },
                    new() { Key = "junction", DisplayName = "Junction", Color = "#3498DB", Icon = "transfer_diverter", Category = "Transport" },
                    new() { Key = "crossdock", DisplayName = "Crossdock", Color = "#16A085", Icon = "crossover", Category = "Transport" },
                    
                    // AGV / Transport
                    new() { Key = "agv", DisplayName = "AGV", Color = "#34495E", Icon = "agv", Category = "Transport" },
                    new() { Key = "agv_station", DisplayName = "AGV Station", Color = "#34495E", Icon = "agv", Category = "Transport" },
                    new() { Key = "forklift", DisplayName = "Forklift", Color = "#FF9800", Icon = "forklift", Category = "Transport" },
                    new() { Key = "cart", DisplayName = "Cart", Color = "#607D8B", Icon = "cart", Category = "Transport" },
                    new() { Key = "crane", DisplayName = "Crane", Color = "#455A64", Icon = "crane", Category = "Transport" },
                    
                    // Vertical Transport
                    new() { Key = "elevator", DisplayName = "Elevator", Color = "#8E44AD", Icon = "elevator", Category = "Transport" },
                    new() { Key = "lift", DisplayName = "Lift", Color = "#8E44AD", Icon = "elevator", Category = "Transport" },
                    
                    // Docking / Shipping
                    new() { Key = "dock", DisplayName = "Dock", Color = "#16A085", Icon = "dock", Category = "Shipping" },
                    new() { Key = "loading_dock", DisplayName = "Loading Dock", Color = "#16A085", Icon = "dock", Category = "Shipping" },
                    new() { Key = "shipping", DisplayName = "Shipping", Color = "#27AE60", Icon = "shipping", Category = "Shipping" },
                    new() { Key = "receiving", DisplayName = "Receiving", Color = "#2980B9", Icon = "receiving", Category = "Shipping" },
                    
                    // People / Operators
                    new() { Key = "operator", DisplayName = "Operator", Color = "#E91E63", Icon = "operator", Category = "People" },
                    new() { Key = "worker", DisplayName = "Worker", Color = "#E91E63", Icon = "operator", Category = "People" },
                }
            };
        }

        /// <summary>
        /// Get the configuration file path
        /// </summary>
        public string GetConfigFilePath() => _configFilePath;
    }
}
