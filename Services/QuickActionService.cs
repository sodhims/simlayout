using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Services
{
    /// <summary>
    /// Service for managing quick actions (common operations accessible from property panel)
    /// </summary>
    public class QuickActionService
    {
        private readonly Dictionary<Type, List<QuickAction>> _actionRegistry = new Dictionary<Type, List<QuickAction>>();

        public QuickActionService()
        {
            RegisterDefaultActions();
        }

        /// <summary>
        /// Register default quick actions for common element types
        /// </summary>
        private void RegisterDefaultActions()
        {
            // Node actions
            RegisterAction<NodeData>(new QuickAction
            {
                Id = "duplicate",
                Name = "Duplicate",
                Description = "Create a copy of this node",
                Icon = "Copy",
                Action = (target, context) =>
                {
                    if (target is NodeData node && context is LayoutData layout)
                    {
                        var copy = new NodeData
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = node.Type
                        };
                        copy.Visual.X = node.Visual.X + 20;
                        copy.Visual.Y = node.Visual.Y + 20;
                        copy.Visual.Width = node.Visual.Width;
                        copy.Visual.Height = node.Visual.Height;
                        layout.Nodes.Add(copy);
                        return (true, "Node duplicated");
                    }
                    return (false, "Invalid target or context");
                }
            });

            RegisterAction<NodeData>(new QuickAction
            {
                Id = "center",
                Name = "Center in View",
                Description = "Center this node in the viewport",
                Icon = "Center",
                Action = (target, context) =>
                {
                    // This would typically interact with a viewport service
                    return (true, "Node centered");
                }
            });

            RegisterAction<NodeData>(new QuickAction
            {
                Id = "reset-size",
                Name = "Reset Size",
                Description = "Reset to default size",
                Icon = "Reset",
                Action = (target, context) =>
                {
                    if (target is NodeData node)
                    {
                        node.Visual.Width = 100;
                        node.Visual.Height = 100;
                        return (true, "Size reset to default");
                    }
                    return (false, "Invalid target");
                }
            });

            // Wall actions
            RegisterAction<WallData>(new QuickAction
            {
                Id = "flip-wall",
                Name = "Flip Direction",
                Description = "Reverse the wall direction",
                Icon = "Flip",
                Action = (target, context) =>
                {
                    if (target is WallData wall)
                    {
                        var temp = wall.X1;
                        wall.X1 = wall.X2;
                        wall.X2 = temp;

                        temp = wall.Y1;
                        wall.Y1 = wall.Y2;
                        wall.Y2 = temp;
                        return (true, "Wall flipped");
                    }
                    return (false, "Invalid target");
                }
            });

            // Path actions
            RegisterAction<PathData>(new QuickAction
            {
                Id = "reverse-path",
                Name = "Reverse Path",
                Description = "Reverse the path direction",
                Icon = "Reverse",
                Action = (target, context) =>
                {
                    if (target is PathData path)
                    {
                        // Swap From and To
                        var temp = path.From;
                        path.From = path.To;
                        path.To = temp;
                        return (true, "Path reversed");
                    }
                    return (false, "Invalid target");
                }
            });
        }

        /// <summary>
        /// Register a quick action for a type
        /// </summary>
        public void RegisterAction<T>(QuickAction action)
        {
            var type = typeof(T);
            if (!_actionRegistry.ContainsKey(type))
            {
                _actionRegistry[type] = new List<QuickAction>();
            }
            _actionRegistry[type].Add(action);
        }

        /// <summary>
        /// Get all available quick actions for an object
        /// </summary>
        public List<QuickAction> GetActionsFor(object target)
        {
            if (target == null)
                return new List<QuickAction>();

            var type = target.GetType();

            // Check for exact type match
            if (_actionRegistry.TryGetValue(type, out var actions))
                return actions.ToList();

            // Check for base types and interfaces
            var allActions = new List<QuickAction>();
            foreach (var kvp in _actionRegistry)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    allActions.AddRange(kvp.Value);
                }
            }

            return allActions;
        }

        /// <summary>
        /// Execute a quick action
        /// </summary>
        public (bool success, string message) ExecuteAction(string actionId, object target, object context = null)
        {
            var actions = GetActionsFor(target);
            var action = actions.FirstOrDefault(a => a.Id == actionId);

            if (action == null)
                return (false, $"Action '{actionId}' not found");

            try
            {
                return action.Action(target, context);
            }
            catch (Exception ex)
            {
                return (false, $"Action failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if an action is available for a target
        /// </summary>
        public bool HasAction(object target, string actionId)
        {
            var actions = GetActionsFor(target);
            return actions.Any(a => a.Id == actionId);
        }

        /// <summary>
        /// Get count of available actions for a target
        /// </summary>
        public int GetActionCount(object target)
        {
            return GetActionsFor(target).Count;
        }
    }

    /// <summary>
    /// Represents a quick action that can be performed on an element
    /// </summary>
    public class QuickAction
    {
        /// <summary>
        /// Unique identifier for the action
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name for the action
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the action does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon identifier for UI
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The action to perform
        /// Parameters: (target object, context object)
        /// Returns: (success, message)
        /// </summary>
        public Func<object, object, (bool success, string message)> Action { get; set; }

        /// <summary>
        /// Whether this action requires confirmation
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Optional keyboard shortcut
        /// </summary>
        public string KeyboardShortcut { get; set; }
    }
}
