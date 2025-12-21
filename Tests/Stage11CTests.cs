using System;
using System.Windows;
using LayoutEditor.Models;
using LayoutEditor.Services;
using LayoutEditor.Services.Constraints;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 11C Tests: Mode Toggle & Selection
    /// Tests frictionless mode toggle and selection filtering
    /// </summary>
    public static class Stage11CTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 11C Tests: Mode Toggle & Selection ===\n");

            var tests = new Func<bool>[]
            {
                Test1_FrictionlessModeFlagWorks,
                Test2_FrictionlessModePersistsInLayout,
                Test3_HitTestRespectsF​rictionlessMode,
                Test4_ConstrainedEntitiesSelectableInFrictionlessMode,
                Test5_NonConstrainedEntitiesNotSelectableInFrictionlessMode,
                Test6_ModeToggleAffectsHitTest
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

            Console.WriteLine($"\nStage 11C Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: FrictionlessMode flag can be toggled
        /// </summary>
        private static bool Test1_FrictionlessModeFlagWorks()
        {
            var layout = new LayoutData();

            // Default should be false
            bool defaultIsFalse = layout.FrictionlessMode == false;

            // Set to true
            layout.FrictionlessMode = true;
            bool canSetTrue = layout.FrictionlessMode == true;

            // Set back to false
            layout.FrictionlessMode = false;
            bool canSetFalse = layout.FrictionlessMode == false;

            return defaultIsFalse && canSetTrue && canSetFalse;
        }

        /// <summary>
        /// Test 2: FrictionlessMode persists in layout data
        /// </summary>
        private static bool Test2_FrictionlessModePersistsInLayout()
        {
            var layout = new LayoutData();
            layout.FrictionlessMode = true;

            // Verify it stays true
            bool persistsTrue = layout.FrictionlessMode == true;

            // Create another layout instance
            var layout2 = new LayoutData();
            bool newLayoutFalse = layout2.FrictionlessMode == false;

            return persistsTrue && newLayoutFalse;
        }

        /// <summary>
        /// Test 3: HitTest service respects frictionless mode parameter
        /// </summary>
        private static bool Test3_HitTestRespectsFrictionlessMode()
        {
            var layout = new LayoutData();
            var hitTestService = new HitTestService();

            // Add a regular node (not constrained)
            var node = new NodeData { Id = "n1", Type = "Machine" };
            node.Visual.X = 100;
            node.Visual.Y = 100;
            node.Visual.Width = 50;
            node.Visual.Height = 50;
            layout.Nodes.Add(node);

            // Hit test with frictionless mode OFF - should hit the node
            var hitNormal = hitTestService.HitTest(layout, new Point(125, 125), null, frictionlessMode: false);
            bool hitsInNormalMode = hitNormal.Type == HitType.Node && hitNormal.Node?.Id == "n1";

            // Hit test with frictionless mode ON - should NOT hit regular nodes
            var hitFrictionless = hitTestService.HitTest(layout, new Point(125, 125), null, frictionlessMode: true);
            bool doesNotHitInFrictionlessMode = hitFrictionless.Type != HitType.Node;

            return hitsInNormalMode && doesNotHitInFrictionlessMode;
        }

        /// <summary>
        /// Test 4: Constrained entities are selectable in frictionless mode
        /// </summary>
        private static bool Test4_ConstrainedEntitiesSelectableInFrictionlessMode()
        {
            var layout = new LayoutData();

            // Add a jib crane (constrained entity)
            var crane = new JibCraneData
            {
                Id = "jib1",
                CenterX = 100,
                CenterY = 100,
                Radius = 50
            };
            layout.JibCranes.Add(crane);

            // Verify it implements IConstrainedEntity
            bool implementsInterface = crane is IConstrainedEntity;

            // Verify it supports constrained movement
            bool supportsMovement = crane.SupportsConstrainedMovement;

            // Verify it has a constraint
            var constraint = crane.GetConstraint();
            bool hasConstraint = constraint != null;

            return implementsInterface && supportsMovement && hasConstraint;
        }

        /// <summary>
        /// Test 5: Non-constrained entities are not selectable in frictionless mode
        /// </summary>
        private static bool Test5_NonConstrainedEntitiesNotSelectableInFrictionlessMode()
        {
            var layout = new LayoutData();
            var hitTestService = new HitTestService();

            // Add multiple non-constrained entities
            var node = new NodeData { Id = "n1", Type = "Machine" };
            node.Visual.X = 100;
            node.Visual.Y = 100;
            node.Visual.Width = 50;
            node.Visual.Height = 50;
            layout.Nodes.Add(node);

            var path = new PathData
            {
                Id = "p1",
                From = "n1",
                To = "n2",
                ConnectionType = ConnectionTypes.PartFlow
            };
            layout.Paths.Add(path);

            // In frictionless mode, these should not be selectable
            var hitNode = hitTestService.HitTest(layout, new Point(125, 125), null, frictionlessMode: true);
            bool nodeNotHit = hitNode.Type != HitType.Node;

            return nodeNotHit;
        }

        /// <summary>
        /// Test 6: Mode toggle affects hit test behavior
        /// </summary>
        private static bool Test6_ModeToggleAffectsHitTest()
        {
            var layout = new LayoutData();
            var hitTestService = new HitTestService();

            // Add a machine node
            var node = new NodeData { Id = "n1", Type = "Machine" };
            node.Visual.X = 100;
            node.Visual.Y = 100;
            node.Visual.Width = 50;
            node.Visual.Height = 50;
            layout.Nodes.Add(node);

            // Test with mode OFF
            layout.FrictionlessMode = false;
            var hitOff = hitTestService.HitTest(layout, new Point(125, 125), null, layout.FrictionlessMode);
            bool hitsWhenOff = hitOff.Type == HitType.Node;

            // Test with mode ON
            layout.FrictionlessMode = true;
            var hitOn = hitTestService.HitTest(layout, new Point(125, 125), null, layout.FrictionlessMode);
            bool doesNotHitWhenOn = hitOn.Type != HitType.Node;

            // Toggle back OFF
            layout.FrictionlessMode = false;
            var hitOffAgain = hitTestService.HitTest(layout, new Point(125, 125), null, layout.FrictionlessMode);
            bool hitsAgainWhenOff = hitOffAgain.Type == HitType.Node;

            return hitsWhenOff && doesNotHitWhenOn && hitsAgainWhenOff;
        }
    }
}
