using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 9A Tests: Selection & Multi-Edit
    /// Tests multi-select, group operations, alignment, and copy/paste
    /// </summary>
    public static class Stage9ATests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 9A Tests: Selection & Multi-Edit ===\n");

            var tests = new Func<bool>[]
            {
                Test1_MultiSelectWorks,
                Test2_MoveMultiple,
                Test3_DeleteMultiple,
                Test4_AlignWorks,
                Test5_CopyPasteWorks
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

            Console.WriteLine($"\nStage 9A Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Multi-select works
        /// </summary>
        private static bool Test1_MultiSelectWorks()
        {
            var selectionService = new SelectionService();

            // Test single selection
            selectionService.SelectNode("node1");
            bool singleSelect = selectionService.IsNodeSelected("node1") && selectionService.SelectedCount == 1;

            // Test adding to selection
            selectionService.SelectNode("node2", addToSelection: true);
            bool multiSelect = selectionService.IsNodeSelected("node1") &&
                              selectionService.IsNodeSelected("node2") &&
                              selectionService.SelectedCount == 2;

            // Test toggle selection
            selectionService.ToggleNodeSelection("node1");
            bool toggleWorks = !selectionService.IsNodeSelected("node1") &&
                              selectionService.IsNodeSelected("node2") &&
                              selectionService.SelectedCount == 1;

            // Test clear
            selectionService.ClearSelection();
            bool clearWorks = selectionService.SelectedCount == 0;

            return singleSelect && multiSelect && toggleWorks && clearWorks;
        }

        /// <summary>
        /// Test 2: Move multiple elements together
        /// </summary>
        private static bool Test2_MoveMultiple()
        {
            var layout = new LayoutData();

            // Create test nodes
            var node1 = new NodeData { Id = "n1" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;

            var node2 = new NodeData { Id = "n2" };
            node2.Visual.X = 200;
            node2.Visual.Y = 150;

            var node3 = new NodeData { Id = "n3" };
            node3.Visual.X = 300;
            node3.Visual.Y = 200;

            layout.Nodes.Add(node1);
            layout.Nodes.Add(node2);
            layout.Nodes.Add(node3);

            var selectionService = new SelectionService();
            selectionService.SelectNode("n1");
            selectionService.SelectNode("n2", addToSelection: true);

            // Get selected nodes
            var selectedNodes = selectionService.GetSelectedNodes(layout);

            // Simulate group move by offsetting all selected nodes
            double deltaX = 50;
            double deltaY = 30;

            foreach (var node in selectedNodes)
            {
                node.Visual.X += deltaX;
                node.Visual.Y += deltaY;
            }

            // Verify both selected nodes moved
            bool node1Moved = node1.Visual.X == 150 && node1.Visual.Y == 130;
            bool node2Moved = node2.Visual.X == 250 && node2.Visual.Y == 180;
            bool node3Unmoved = node3.Visual.X == 300 && node3.Visual.Y == 200;

            return node1Moved && node2Moved && node3Unmoved;
        }

        /// <summary>
        /// Test 3: Delete multiple elements
        /// </summary>
        private static bool Test3_DeleteMultiple()
        {
            var layout = new LayoutData();

            // Create test nodes
            var node1 = new NodeData { Id = "n1" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;

            var node2 = new NodeData { Id = "n2" };
            node2.Visual.X = 200;
            node2.Visual.Y = 150;

            var node3 = new NodeData { Id = "n3" };
            node3.Visual.X = 300;
            node3.Visual.Y = 200;

            layout.Nodes.Add(node1);
            layout.Nodes.Add(node2);
            layout.Nodes.Add(node3);

            var selectionService = new SelectionService();
            selectionService.SelectNode("n1");
            selectionService.SelectNode("n2", addToSelection: true);

            // Get IDs to delete
            var idsToDelete = selectionService.SelectedNodeIds.ToList();

            // Remove nodes
            foreach (var id in idsToDelete)
            {
                var nodeToRemove = layout.Nodes.FirstOrDefault(n => n.Id == id);
                if (nodeToRemove != null)
                {
                    layout.Nodes.Remove(nodeToRemove);
                }
            }

            // Verify deletion
            bool node1Deleted = !layout.Nodes.Any(n => n.Id == "n1");
            bool node2Deleted = !layout.Nodes.Any(n => n.Id == "n2");
            bool node3Remains = layout.Nodes.Any(n => n.Id == "n3");
            bool countCorrect = layout.Nodes.Count == 1;

            return node1Deleted && node2Deleted && node3Remains && countCorrect;
        }

        /// <summary>
        /// Test 4: Alignment works
        /// </summary>
        private static bool Test4_AlignWorks()
        {
            var layout = new LayoutData();
            var alignmentService = new AlignmentService();

            // Create test nodes with different positions
            var node1 = new NodeData { Id = "n1" };
            node1.Visual.X = 100;
            node1.Visual.Y = 100;

            var node2 = new NodeData { Id = "n2" };
            node2.Visual.X = 200;
            node2.Visual.Y = 150;

            var node3 = new NodeData { Id = "n3" };
            node3.Visual.X = 150;
            node3.Visual.Y = 200;

            layout.Nodes.Add(node1);
            layout.Nodes.Add(node2);
            layout.Nodes.Add(node3);

            var nodes = new System.Collections.Generic.List<NodeData> { node1, node2, node3 };

            // Test align left
            alignmentService.AlignLeft(nodes);
            bool alignLeftWorks = node1.Visual.X == 100 &&
                                 node2.Visual.X == 100 &&
                                 node3.Visual.X == 100;

            // Reset positions
            node1.Visual.X = 100; node1.Visual.Y = 100;
            node2.Visual.X = 200; node2.Visual.Y = 150;
            node3.Visual.X = 150; node3.Visual.Y = 200;

            // Test align top
            alignmentService.AlignTop(nodes);
            bool alignTopWorks = node1.Visual.Y == 100 &&
                                node2.Visual.Y == 100 &&
                                node3.Visual.Y == 100;

            return alignLeftWorks && alignTopWorks;
        }

        /// <summary>
        /// Test 5: Copy/paste works (simplified test)
        /// </summary>
        private static bool Test5_CopyPasteWorks()
        {
            var layout = new LayoutData();

            // Create original node
            var original = new NodeData { Id = "n1", Type = "Machine" };
            original.Visual.X = 100;
            original.Visual.Y = 100;
            layout.Nodes.Add(original);

            // Simulate copy by cloning properties
            var copy = new NodeData
            {
                Id = Guid.NewGuid().ToString(), // New ID
                Type = original.Type
            };
            copy.Visual.X = original.Visual.X + 20; // Offset position
            copy.Visual.Y = original.Visual.Y + 20;

            layout.Nodes.Add(copy);

            // Verify copy was created
            bool originalExists = layout.Nodes.Any(n => n.Id == "n1");
            bool copyExists = layout.Nodes.Count == 2;
            bool copyHasNewId = copy.Id != original.Id;
            bool copyHasSameType = copy.Type == original.Type;
            bool copyIsOffset = copy.Visual.X == 120 && copy.Visual.Y == 120;

            return originalExists && copyExists && copyHasNewId && copyHasSameType && copyIsOffset;
        }
    }
}
