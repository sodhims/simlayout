using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 9D Tests: Property Panel Enhancements
    /// Tests dynamic property discovery, validation, and quick actions
    /// </summary>
    public static class Stage9DTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 9D Tests: Property Panel Enhancements ===\n");

            var tests = new Func<bool>[]
            {
                Test1_PropertyDiscovery,
                Test2_PropertyValidation,
                Test3_QuickActions
            };

            int passed = 0;
            int failed = 0;

            for (int i = 0; i < tests.Length; i++)
            {
                try
                {
                    bool result = tests[i]();
                    if (result)
                    {
                        passed++;
                        Console.WriteLine($"✓ Test {i + 1} passed");
                    }
                    else
                    {
                        failed++;
                        Console.WriteLine($"✗ Test {i + 1} failed");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"✗ Test {i + 1} failed with exception: {ex.Message}");
                }
            }

            Console.WriteLine($"\nStage 9D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Property discovery works for different types
        /// </summary>
        private static bool Test1_PropertyDiscovery()
        {
            var propertyService = new PropertyPanelService();

            // Test with NodeData
            var node = new NodeData { Id = "n1", Type = "Machine" };
            var nodeProps = propertyService.GetEditableProperties(node);

            // Should discover properties like Id, Type, etc.
            bool hasProperties = nodeProps.Count > 0;
            bool hasIdProperty = nodeProps.Any(p => p.Name == "Id");
            bool hasTypeProperty = nodeProps.Any(p => p.Name == "Type");

            // Test getting property value
            var idProp = nodeProps.FirstOrDefault(p => p.Name == "Id");
            object idValue = null;
            if (idProp != null)
            {
                idValue = propertyService.GetPropertyValue(node, idProp);
            }
            bool correctValue = idValue?.ToString() == "n1";

            // Test with WallData
            var wall = new WallData { Id = "w1" };
            var wallProps = propertyService.GetEditableProperties(wall);
            bool wallHasProperties = wallProps.Count > 0;

            return hasProperties && hasIdProperty && hasTypeProperty &&
                   correctValue && wallHasProperties;
        }

        /// <summary>
        /// Test 2: Property validation works
        /// </summary>
        private static bool Test2_PropertyValidation()
        {
            var propertyService = new PropertyPanelService();
            var node = new NodeData { Id = "n1" };
            var props = propertyService.GetEditableProperties(node);

            // Get the Type property
            var typeProp = props.FirstOrDefault(p => p.Name == "Type");
            if (typeProp == null)
            {
                // If Type is not editable, test with Id instead
                typeProp = props.FirstOrDefault(p => p.Name == "Id");
            }

            if (typeProp != null)
            {
                // Test setting valid value
                var result1 = propertyService.SetPropertyValue(node, typeProp, "TestType");
                bool validValueSet = result1.success;

                // Verify value was actually set
                var newValue = propertyService.GetPropertyValue(node, typeProp);
                bool valueChanged = newValue?.ToString() == "TestType";

                // Test with range validator
                var rangeValidator = new RangeValidator(0, 100);
                var validResult = rangeValidator.Validate(50);
                bool validRangePasses = validResult.isValid;

                var invalidResult = rangeValidator.Validate(150);
                bool invalidRangeFails = !invalidResult.isValid;

                // Test with required validator
                var requiredValidator = new RequiredValidator();
                var requiredValid = requiredValidator.Validate("SomeValue");
                bool requiredPasses = requiredValid.isValid;

                var requiredInvalid = requiredValidator.Validate(null);
                bool requiredFails = !requiredInvalid.isValid;

                return validValueSet && valueChanged && validRangePasses &&
                       invalidRangeFails && requiredPasses && requiredFails;
            }

            return false;
        }

        /// <summary>
        /// Test 3: Quick actions work
        /// </summary>
        private static bool Test3_QuickActions()
        {
            var quickActionService = new QuickActionService();
            var layout = new LayoutData();

            // Create a node
            var node = new NodeData { Id = "n1", Type = "Machine" };
            node.Visual.X = 100;
            node.Visual.Y = 100;
            node.Visual.Width = 50;
            node.Visual.Height = 50;
            layout.Nodes.Add(node);

            // Get available actions for node
            var actions = quickActionService.GetActionsFor(node);
            bool hasActions = actions.Count > 0;

            // Check for specific actions
            bool hasDuplicateAction = quickActionService.HasAction(node, "duplicate");
            bool hasResetSizeAction = quickActionService.HasAction(node, "reset-size");

            // Execute duplicate action
            int initialCount = layout.Nodes.Count;
            var dupResult = quickActionService.ExecuteAction("duplicate", node, layout);
            bool duplicateSuccess = dupResult.success && layout.Nodes.Count == initialCount + 1;

            // Verify the duplicated node is offset
            var duplicatedNode = layout.Nodes.LastOrDefault();
            bool duplicatedIsOffset = duplicatedNode != null &&
                                     duplicatedNode.Visual.X == 120 &&
                                     duplicatedNode.Visual.Y == 120;

            // Execute reset-size action
            node.Visual.Width = 200;
            node.Visual.Height = 300;
            var resetResult = quickActionService.ExecuteAction("reset-size", node);
            bool resetSuccess = resetResult.success &&
                               node.Visual.Width == 100 &&
                               node.Visual.Height == 100;

            // Test with wall
            var wall = new WallData
            {
                Id = "w1",
                X1 = 0,
                Y1 = 0,
                X2 = 100,
                Y2 = 100
            };
            var wallActions = quickActionService.GetActionsFor(wall);
            bool wallHasActions = wallActions.Count > 0;

            // Execute flip-wall action
            var flipResult = quickActionService.ExecuteAction("flip-wall", wall);
            bool flipSuccess = flipResult.success &&
                              wall.X1 == 100 &&
                              wall.X2 == 0;

            return hasActions && hasDuplicateAction && hasResetSizeAction &&
                   duplicateSuccess && duplicatedIsOffset && resetSuccess &&
                   wallHasActions && flipSuccess;
        }
    }
}
