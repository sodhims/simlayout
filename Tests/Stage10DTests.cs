using System;
using System.Diagnostics;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Stage 10D Tests: Database Integration
    /// Tests query performance and completeness checking
    /// </summary>
    public static class Stage10DTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Stage 10D Tests: Database Integration ===\n");

            var tests = new Func<bool>[]
            {
                Test1_QueriesPerformant,
                Test2_CompletenessCheckWorks
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

            Console.WriteLine($"\nStage 10D Results: {passed} passed, {failed} failed out of {tests.Length} tests");
            return failed == 0;
        }

        /// <summary>
        /// Test 1: Queries are performant (< 100ms for typical queries)
        /// </summary>
        private static bool Test1_QueriesPerformant()
        {
            var layout = CreateLargeLayout();
            var queryService = new LayoutQueryService();

            var stopwatch = new Stopwatch();

            // Test GetWorkstations performance
            stopwatch.Restart();
            var workstations = queryService.GetWorkstations(layout);
            stopwatch.Stop();
            bool getWorkstationsFast = stopwatch.ElapsedMilliseconds < 100;
            Console.WriteLine($"  GetWorkstations: {stopwatch.ElapsedMilliseconds}ms");

            // Test GetElementsInZone performance
            var zone = layout.Zones.FirstOrDefault();
            stopwatch.Restart();
            var elementsInZone = queryService.GetElementsInZone(layout, zone?.Name);
            stopwatch.Stop();
            bool getElementsInZoneFast = stopwatch.ElapsedMilliseconds < 100;
            Console.WriteLine($"  GetElementsInZone: {stopwatch.ElapsedMilliseconds}ms");

            // Test GetElementsInRegion performance
            stopwatch.Restart();
            var elementsInRegion = queryService.GetElementsInRegion(layout, 0, 0, 500, 500);
            stopwatch.Stop();
            bool getElementsInRegionFast = stopwatch.ElapsedMilliseconds < 100;
            Console.WriteLine($"  GetElementsInRegion: {stopwatch.ElapsedMilliseconds}ms");

            // Test GetNearestWorkstation performance
            stopwatch.Restart();
            var nearest = queryService.GetNearestWorkstation(layout, 250, 250);
            stopwatch.Stop();
            bool getNearestFast = stopwatch.ElapsedMilliseconds < 100;
            Console.WriteLine($"  GetNearestWorkstation: {stopwatch.ElapsedMilliseconds}ms");

            // Test GetConnectedTransport performance
            var nodeId = layout.Nodes.FirstOrDefault()?.Id;
            stopwatch.Restart();
            var connected = queryService.GetConnectedTransport(layout, nodeId);
            stopwatch.Stop();
            bool getConnectedFast = stopwatch.ElapsedMilliseconds < 100;
            Console.WriteLine($"  GetConnectedTransport: {stopwatch.ElapsedMilliseconds}ms");

            return getWorkstationsFast && getElementsInZoneFast &&
                   getElementsInRegionFast && getNearestFast && getConnectedFast;
        }

        /// <summary>
        /// Test 2: Completeness check identifies missing connections
        /// </summary>
        private static bool Test2_CompletenessCheckWorks()
        {
            var completenessService = new LayoutCompletenessService();

            // Create layout with completeness issues
            var layout = new LayoutData();

            // Add orphaned node (no connections)
            var orphan = new NodeData { Id = "orphan", Type = "Machine" };
            orphan.Visual.X = 100;
            orphan.Visual.Y = 100;
            layout.Nodes.Add(orphan);

            // Add connected nodes
            var node1 = new NodeData { Id = "n1", Type = "Station" };
            node1.Visual.X = 200;
            node1.Visual.Y = 200;

            var node2 = new NodeData { Id = "n2", Type = "Station" };
            node2.Visual.X = 300;
            node2.Visual.Y = 300;

            layout.Nodes.Add(node1);
            layout.Nodes.Add(node2);

            // Add path with broken reference
            var brokenPath = new PathData
            {
                Id = "broken",
                From = "n1",
                To = "nonexistent", // Broken reference
                ConnectionType = ConnectionTypes.PartFlow
            };
            layout.Paths.Add(brokenPath);

            // Add valid path
            var validPath = new PathData
            {
                Id = "valid",
                From = "n1",
                To = "n2",
                ConnectionType = ConnectionTypes.PartFlow
            };
            layout.Paths.Add(validPath);

            // Add one-way AGV path (no return)
            var agvPath = new PathData
            {
                Id = "agv1",
                From = "n1",
                To = "n2",
                ConnectionType = ConnectionTypes.AGVTrack
            };
            layout.Paths.Add(agvPath);

            // Add adjacent cranes without handoff
            var crane1 = new NodeData { Id = "cr1", Type = "EOTCrane" };
            crane1.Visual.X = 400;
            crane1.Visual.Y = 400;

            var crane2 = new NodeData { Id = "cr2", Type = "EOTCrane" };
            crane2.Visual.X = 450; // Close to crane1
            crane2.Visual.Y = 450;

            layout.Nodes.Add(crane1);
            layout.Nodes.Add(crane2);

            // Check completeness
            var report = completenessService.CheckCompleteness(layout);

            // Verify report findings
            bool isNotComplete = !report.IsComplete;
            bool hasIssues = report.TotalIssues > 0;
            bool hasErrors = report.ErrorCount > 0; // Broken reference
            bool hasWarnings = report.WarningCount > 0; // Orphaned node, missing return path
            bool hasOrphanIssue = report.Issues.Any(i => i.Category == "Connectivity" && i.ElementId == "orphan");
            bool hasBrokenRefIssue = report.Issues.Any(i => i.Category == "Broken Reference" && i.ElementId == "broken");
            bool hasMissingReturnIssue = report.Issues.Any(i => i.Category == "Missing Return Path");

            // Test statistics
            var stats = completenessService.GetStatistics(layout);
            bool correctNodeCount = stats.TotalNodes == 5; // orphan, n1, n2, cr1, cr2
            bool correctPathCount = stats.TotalPaths == 3;
            bool correctWorkstationCount = stats.WorkstationCount == 3; // orphan, n1, n2
            bool correctCraneCount = stats.CraneCount == 2;

            Console.WriteLine($"  Report: {report.TotalIssues} issues ({report.ErrorCount} errors, {report.WarningCount} warnings, {report.InfoCount} info)");
            Console.WriteLine($"  Statistics: {stats.TotalNodes} nodes, {stats.WorkstationCount} workstations, {stats.CraneCount} cranes");

            return isNotComplete && hasIssues && hasErrors && hasWarnings &&
                   hasOrphanIssue && hasBrokenRefIssue && hasMissingReturnIssue &&
                   correctNodeCount && correctPathCount && correctWorkstationCount && correctCraneCount;
        }

        /// <summary>
        /// Helper to create a large layout for performance testing
        /// </summary>
        private static LayoutData CreateLargeLayout()
        {
            var layout = new LayoutData();

            // Create 50 workstations
            for (int i = 0; i < 50; i++)
            {
                var node = new NodeData { Id = $"m{i}", Type = "Machine" };
                node.Visual.X = (i % 10) * 100;
                node.Visual.Y = (i / 10) * 100;
                node.Visual.Width = 50;
                node.Visual.Height = 50;
                layout.Nodes.Add(node);
            }

            // Create 100 paths
            for (int i = 0; i < 50; i++)
            {
                var path = new PathData
                {
                    Id = $"p{i}",
                    From = $"m{i}",
                    To = $"m{(i + 1) % 50}",
                    ConnectionType = ConnectionTypes.PartFlow
                };
                layout.Paths.Add(path);

                // Add return path
                var returnPath = new PathData
                {
                    Id = $"pr{i}",
                    From = $"m{(i + 1) % 50}",
                    To = $"m{i}",
                    ConnectionType = ConnectionTypes.PartFlow
                };
                layout.Paths.Add(returnPath);
            }

            // Create 10 zones
            for (int i = 0; i < 10; i++)
            {
                var zone = new ZoneData
                {
                    Id = $"z{i}",
                    Name = $"Zone {i}",
                    X = (i % 5) * 200,
                    Y = (i / 5) * 250,
                    Width = 200,
                    Height = 250
                };
                layout.Zones.Add(zone);
            }

            return layout;
        }
    }
}
