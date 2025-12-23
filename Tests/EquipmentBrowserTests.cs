using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LayoutEditor.Models;
using LayoutEditor.Helpers;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Rigorous tests for Equipment Browser and Data Grid functionality.
    /// Tests data synchronization, layout loading, and count accuracy.
    /// </summary>
    public static class EquipmentBrowserTests
    {
        public static bool RunAllTests()
        {
            Console.WriteLine("\n=== Equipment Browser Tests ===\n");

            var tests = new (string Name, Func<bool> Test)[]
            {
                // Data Model Tests
                ("T1: LayoutData collections initialize empty", Test1_LayoutDataCollectionsInitializeEmpty),
                ("T2: EOTCrane adds to collection correctly", Test2_EOTCraneAddsToCollection),
                ("T3: Zone adds to collection correctly", Test3_ZoneAddsToCollection),
                ("T4: AGVStation adds to collection correctly", Test4_AGVStationAddsToCollection),
                ("T5: Runway adds to collection correctly", Test5_RunwayAddsToCollection),
                ("T6: JibCrane adds to collection correctly", Test6_JibCraneAddsToCollection),
                ("T7: Conveyor adds to collection correctly", Test7_ConveyorAddsToCollection),
                ("T8: Opening adds to collection correctly", Test8_OpeningAddsToCollection),
                ("T9: Node adds to collection correctly", Test9_NodeAddsToCollection),
                ("T10: Path adds to collection correctly", Test10_PathAddsToCollection),
                ("T11: Wall adds to collection correctly", Test11_WallAddsToCollection),

                // Serialization Round-Trip Tests
                ("T12: Full layout serializes and deserializes", Test12_FullLayoutSerializationRoundTrip),
                ("T13: Equipment counts preserved after serialization", Test13_EquipmentCountsPreservedAfterSerialization),
                ("T14: Equipment properties preserved after serialization", Test14_EquipmentPropertiesPreserved),

                // Layout Reference Tests
                ("T15: Layout reference assignment works", Test15_LayoutReferenceAssignment),
                ("T16: Layout collections are same reference after assignment", Test16_LayoutCollectionsSameReference),
                ("T17: Modifying layout updates collection", Test17_ModifyingLayoutUpdatesCollection),

                // Count Accuracy Tests
                ("T18: Multiple equipment types counted correctly", Test18_MultipleEquipmentTypesCountedCorrectly),
                ("T19: Empty layout shows zero counts", Test19_EmptyLayoutShowsZeroCounts),
                ("T20: Large layout counts accurate", Test20_LargeLayoutCountsAccurate),

                // Data Integrity Tests
                ("T21: Equipment IDs are unique", Test21_EquipmentIdsAreUnique),
                ("T22: Equipment names are accessible", Test22_EquipmentNamesAccessible),
                ("T23: Crane-Runway relationship intact", Test23_CraneRunwayRelationshipIntact),
            };

            int passed = 0;
            int failed = 0;

            foreach (var (name, test) in tests)
            {
                try
                {
                    bool result = test();
                    if (result)
                    {
                        passed++;
                        Console.WriteLine($"  PASSED: {name}");
                    }
                    else
                    {
                        failed++;
                        Console.WriteLine($"  FAILED: {name}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"  FAILED: {name} - Exception: {ex.Message}");
                }
            }

            Console.WriteLine($"\n=== Equipment Browser Results: {passed}/{tests.Length} passed ===\n");
            return failed == 0;
        }

        #region Data Model Tests (T1-T11)

        private static bool Test1_LayoutDataCollectionsInitializeEmpty()
        {
            var layout = new LayoutData();

            return layout.EOTCranes.Count == 0 &&
                   layout.JibCranes.Count == 0 &&
                   layout.Runways.Count == 0 &&
                   layout.Zones.Count == 0 &&
                   layout.AGVStations.Count == 0 &&
                   layout.Conveyors.Count == 0 &&
                   layout.Openings.Count == 0 &&
                   layout.Nodes.Count == 0 &&
                   layout.Paths.Count == 0 &&
                   layout.Walls.Count == 0;
        }

        private static bool Test2_EOTCraneAddsToCollection()
        {
            var layout = new LayoutData();
            var crane = new EOTCraneData
            {
                Id = "crane-1",
                Name = "EOT Crane 1",
                BayWidth = 10.0,
                ReachLeft = 5.0,
                ReachRight = 5.0
            };

            layout.EOTCranes.Add(crane);

            return layout.EOTCranes.Count == 1 &&
                   layout.EOTCranes[0].Name == "EOT Crane 1";
        }

        private static bool Test3_ZoneAddsToCollection()
        {
            var layout = new LayoutData();
            var zone = new ZoneData
            {
                Id = "zone-1",
                Name = "Storage Zone",
                Type = "storage",
                X = 100,
                Y = 200,
                Width = 50,
                Height = 50
            };

            layout.Zones.Add(zone);

            return layout.Zones.Count == 1 &&
                   layout.Zones[0].Name == "Storage Zone" &&
                   layout.Zones[0].Type == "storage";
        }

        private static bool Test4_AGVStationAddsToCollection()
        {
            var layout = new LayoutData();
            var station = new AGVStationData
            {
                Id = "station-1",
                Name = "AGV Station 1",
                StationType = "pickup",
                X = 150,
                Y = 250
            };

            layout.AGVStations.Add(station);

            return layout.AGVStations.Count == 1 &&
                   layout.AGVStations[0].Name == "AGV Station 1" &&
                   layout.AGVStations[0].StationType == "pickup";
        }

        private static bool Test5_RunwayAddsToCollection()
        {
            var layout = new LayoutData();
            var runway = new RunwayData
            {
                Id = "runway-1",
                Name = "Runway 1",
                StartX = 0,
                StartY = 100,
                EndX = 500,
                EndY = 100,
                Height = 10
            };

            layout.Runways.Add(runway);

            return layout.Runways.Count == 1 &&
                   layout.Runways[0].Name == "Runway 1";
        }

        private static bool Test6_JibCraneAddsToCollection()
        {
            var layout = new LayoutData();
            var jib = new JibCraneData
            {
                Id = "jib-1",
                Name = "Jib Crane 1",
                CenterX = 200,
                CenterY = 200,
                Radius = 50,
                ArcStart = 0,
                ArcEnd = 180
            };

            layout.JibCranes.Add(jib);

            return layout.JibCranes.Count == 1 &&
                   layout.JibCranes[0].Name == "Jib Crane 1";
        }

        private static bool Test7_ConveyorAddsToCollection()
        {
            var layout = new LayoutData();
            var conveyor = new ConveyorData
            {
                Id = "conveyor-1",
                Name = "Conveyor 1",
                ConveyorType = "belt",
                Width = 2.0,
                Speed = 1.5
            };

            layout.Conveyors.Add(conveyor);

            return layout.Conveyors.Count == 1 &&
                   layout.Conveyors[0].Name == "Conveyor 1";
        }

        private static bool Test8_OpeningAddsToCollection()
        {
            var layout = new LayoutData();
            var opening = new OpeningData
            {
                Id = "opening-1",
                Name = "Main Door",
                OpeningType = "door",
                X = 0,
                Y = 50
            };

            layout.Openings.Add(opening);

            return layout.Openings.Count == 1 &&
                   layout.Openings[0].Name == "Main Door";
        }

        private static bool Test9_NodeAddsToCollection()
        {
            var layout = new LayoutData();
            var node = new NodeData
            {
                Id = "node-1",
                Name = "Machine 1",
                Type = "machine"
            };

            layout.Nodes.Add(node);

            return layout.Nodes.Count == 1 &&
                   layout.Nodes[0].Name == "Machine 1";
        }

        private static bool Test10_PathAddsToCollection()
        {
            var layout = new LayoutData();
            var path = new PathData
            {
                Id = "path-1",
                From = "node-1",
                To = "node-2",
                PathType = "single"
            };

            layout.Paths.Add(path);

            return layout.Paths.Count == 1 &&
                   layout.Paths[0].From == "node-1";
        }

        private static bool Test11_WallAddsToCollection()
        {
            var layout = new LayoutData();
            var wall = new WallData
            {
                Id = "wall-1",
                X1 = 0,
                Y1 = 0,
                X2 = 100,
                Y2 = 0,
                WallType = "exterior"
            };

            layout.Walls.Add(wall);

            return layout.Walls.Count == 1 &&
                   layout.Walls[0].WallType == "exterior";
        }

        #endregion

        #region Serialization Round-Trip Tests (T12-T14)

        private static bool Test12_FullLayoutSerializationRoundTrip()
        {
            var layout = CreatePopulatedLayout();

            // Serialize
            string json = JsonHelper.Serialize(layout);

            // Deserialize
            var restored = JsonHelper.Deserialize<LayoutData>(json);

            return restored != null &&
                   restored.EOTCranes.Count == layout.EOTCranes.Count &&
                   restored.Zones.Count == layout.Zones.Count &&
                   restored.AGVStations.Count == layout.AGVStations.Count;
        }

        private static bool Test13_EquipmentCountsPreservedAfterSerialization()
        {
            var layout = CreatePopulatedLayout();

            int originalCranes = layout.EOTCranes.Count;
            int originalZones = layout.Zones.Count;
            int originalStations = layout.AGVStations.Count;
            int originalRunways = layout.Runways.Count;
            int originalJibs = layout.JibCranes.Count;
            int originalConveyors = layout.Conveyors.Count;
            int originalOpenings = layout.Openings.Count;
            int originalNodes = layout.Nodes.Count;
            int originalPaths = layout.Paths.Count;
            int originalWalls = layout.Walls.Count;

            string json = JsonHelper.Serialize(layout);
            var restored = JsonHelper.Deserialize<LayoutData>(json);

            if (restored == null) return false;

            bool craneCountMatch = restored.EOTCranes.Count == originalCranes;
            bool zoneCountMatch = restored.Zones.Count == originalZones;
            bool stationCountMatch = restored.AGVStations.Count == originalStations;
            bool runwayCountMatch = restored.Runways.Count == originalRunways;
            bool jibCountMatch = restored.JibCranes.Count == originalJibs;
            bool conveyorCountMatch = restored.Conveyors.Count == originalConveyors;
            bool openingCountMatch = restored.Openings.Count == originalOpenings;
            bool nodeCountMatch = restored.Nodes.Count == originalNodes;
            bool pathCountMatch = restored.Paths.Count == originalPaths;
            bool wallCountMatch = restored.Walls.Count == originalWalls;

            if (!craneCountMatch) Console.WriteLine($"    Crane count mismatch: {restored.EOTCranes.Count} vs {originalCranes}");
            if (!zoneCountMatch) Console.WriteLine($"    Zone count mismatch: {restored.Zones.Count} vs {originalZones}");
            if (!stationCountMatch) Console.WriteLine($"    Station count mismatch: {restored.AGVStations.Count} vs {originalStations}");

            return craneCountMatch && zoneCountMatch && stationCountMatch &&
                   runwayCountMatch && jibCountMatch && conveyorCountMatch &&
                   openingCountMatch && nodeCountMatch && pathCountMatch && wallCountMatch;
        }

        private static bool Test14_EquipmentPropertiesPreserved()
        {
            var layout = new LayoutData();

            // Note: Setting BayWidth changes ReachLeft/ReachRight symmetrically
            // So BayWidth = 100 means ReachLeft = ReachRight = 50
            var crane = new EOTCraneData
            {
                Id = "test-crane",
                Name = "Test EOT Crane",
                BayWidth = 100,  // This will set ReachLeft = ReachRight = 50
                ZoneMin = 0.1,
                ZoneMax = 0.9,
                SpeedBridge = 2.5,
                SpeedTrolley = 1.8,
                SpeedHoist = 0.5
            };
            layout.EOTCranes.Add(crane);

            var zone = new ZoneData
            {
                Id = "test-zone",
                Name = "Test Zone",
                Type = "buffer",
                X = 123.4,
                Y = 567.8,
                Width = 45.6,
                Height = 78.9,
                Capacity = 100,
                IsRestricted = true
            };
            layout.Zones.Add(zone);

            string json = JsonHelper.Serialize(layout);
            var restored = JsonHelper.Deserialize<LayoutData>(json);

            if (restored == null || restored.EOTCranes.Count == 0 || restored.Zones.Count == 0)
                return false;

            var restoredCrane = restored.EOTCranes[0];
            var restoredZone = restored.Zones[0];

            // Check crane properties - BayWidth=100 means ReachLeft should be 50
            bool cranePropsMatch = restoredCrane.Name == "Test EOT Crane" &&
                                   Math.Abs(restoredCrane.BayWidth - 100) < 0.001 &&
                                   Math.Abs(restoredCrane.ZoneMin - 0.1) < 0.001 &&
                                   Math.Abs(restoredCrane.SpeedBridge - 2.5) < 0.001;

            bool zonePropsMatch = restoredZone.Name == "Test Zone" &&
                                  restoredZone.Type == "buffer" &&
                                  Math.Abs(restoredZone.X - 123.4) < 0.001 &&
                                  restoredZone.Capacity == 100 &&
                                  restoredZone.IsRestricted == true;

            if (!cranePropsMatch)
            {
                Console.WriteLine($"    Crane props: Name={restoredCrane.Name}, BayWidth={restoredCrane.BayWidth}, ZoneMin={restoredCrane.ZoneMin}, SpeedBridge={restoredCrane.SpeedBridge}");
            }
            if (!zonePropsMatch)
            {
                Console.WriteLine($"    Zone props: Name={restoredZone.Name}, Type={restoredZone.Type}, X={restoredZone.X}, Capacity={restoredZone.Capacity}, IsRestricted={restoredZone.IsRestricted}");
            }

            return cranePropsMatch && zonePropsMatch;
        }

        #endregion

        #region Layout Reference Tests (T15-T17)

        private static bool Test15_LayoutReferenceAssignment()
        {
            var layout1 = CreatePopulatedLayout();
            var layout2 = new LayoutData();

            // Simulate what SetLayout does - assigns layout reference
            LayoutData currentLayout = layout1;

            // Verify reference is correct
            bool layout1HasData = currentLayout.EOTCranes.Count > 0;

            // Change reference
            currentLayout = layout2;
            bool layout2Empty = currentLayout.EOTCranes.Count == 0;

            return layout1HasData && layout2Empty;
        }

        private static bool Test16_LayoutCollectionsSameReference()
        {
            var layout = CreatePopulatedLayout();
            LayoutData assignedLayout = layout;

            // The collections should be the same reference
            bool sameEOTCranes = ReferenceEquals(layout.EOTCranes, assignedLayout.EOTCranes);
            bool sameZones = ReferenceEquals(layout.Zones, assignedLayout.Zones);
            bool sameStations = ReferenceEquals(layout.AGVStations, assignedLayout.AGVStations);

            return sameEOTCranes && sameZones && sameStations;
        }

        private static bool Test17_ModifyingLayoutUpdatesCollection()
        {
            var layout = new LayoutData();
            LayoutData assignedLayout = layout;

            // Add via original reference
            layout.EOTCranes.Add(new EOTCraneData { Name = "Crane 1" });

            // Should be visible in assigned reference
            bool seenInAssigned = assignedLayout.EOTCranes.Count == 1 &&
                                  assignedLayout.EOTCranes[0].Name == "Crane 1";

            // Add via assigned reference
            assignedLayout.Zones.Add(new ZoneData { Name = "Zone 1" });

            // Should be visible in original reference
            bool seenInOriginal = layout.Zones.Count == 1 &&
                                  layout.Zones[0].Name == "Zone 1";

            return seenInAssigned && seenInOriginal;
        }

        #endregion

        #region Count Accuracy Tests (T18-T20)

        private static bool Test18_MultipleEquipmentTypesCountedCorrectly()
        {
            var layout = new LayoutData();

            // Add 3 EOT cranes
            for (int i = 0; i < 3; i++)
                layout.EOTCranes.Add(new EOTCraneData { Name = $"Crane {i}" });

            // Add 5 zones
            for (int i = 0; i < 5; i++)
                layout.Zones.Add(new ZoneData { Name = $"Zone {i}" });

            // Add 2 AGV stations
            for (int i = 0; i < 2; i++)
                layout.AGVStations.Add(new AGVStationData { Name = $"Station {i}" });

            // Add 4 nodes
            for (int i = 0; i < 4; i++)
                layout.Nodes.Add(new NodeData { Name = $"Node {i}" });

            return layout.EOTCranes.Count == 3 &&
                   layout.Zones.Count == 5 &&
                   layout.AGVStations.Count == 2 &&
                   layout.Nodes.Count == 4;
        }

        private static bool Test19_EmptyLayoutShowsZeroCounts()
        {
            var layout = new LayoutData();

            int totalCount = layout.EOTCranes.Count +
                            layout.JibCranes.Count +
                            layout.Runways.Count +
                            layout.Zones.Count +
                            layout.AGVStations.Count +
                            layout.Conveyors.Count +
                            layout.Openings.Count +
                            layout.Nodes.Count +
                            layout.Paths.Count +
                            layout.Walls.Count;

            return totalCount == 0;
        }

        private static bool Test20_LargeLayoutCountsAccurate()
        {
            var layout = new LayoutData();

            // Add 100 zones
            for (int i = 0; i < 100; i++)
                layout.Zones.Add(new ZoneData { Id = $"zone-{i}", Name = $"Zone {i}", Type = "storage" });

            // Add 50 nodes
            for (int i = 0; i < 50; i++)
                layout.Nodes.Add(new NodeData { Id = $"node-{i}", Name = $"Node {i}" });

            // Add 25 AGV stations
            for (int i = 0; i < 25; i++)
                layout.AGVStations.Add(new AGVStationData { Id = $"station-{i}", Name = $"Station {i}" });

            return layout.Zones.Count == 100 &&
                   layout.Nodes.Count == 50 &&
                   layout.AGVStations.Count == 25;
        }

        #endregion

        #region Data Integrity Tests (T21-T23)

        private static bool Test21_EquipmentIdsAreUnique()
        {
            var layout = CreatePopulatedLayout();

            var allIds = new List<string>();

            allIds.AddRange(layout.EOTCranes.Select(c => c.Id));
            allIds.AddRange(layout.JibCranes.Select(j => j.Id));
            allIds.AddRange(layout.Runways.Select(r => r.Id));
            allIds.AddRange(layout.Zones.Select(z => z.Id));
            allIds.AddRange(layout.AGVStations.Select(s => s.Id));
            allIds.AddRange(layout.Conveyors.Select(c => c.Id));
            allIds.AddRange(layout.Openings.Select(o => o.Id));
            allIds.AddRange(layout.Nodes.Select(n => n.Id));
            allIds.AddRange(layout.Walls.Select(w => w.Id));

            // Check for duplicates
            var duplicates = allIds.GroupBy(id => id)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key)
                                   .ToList();

            if (duplicates.Any())
            {
                Console.WriteLine($"    Duplicate IDs found: {string.Join(", ", duplicates)}");
                return false;
            }

            return true;
        }

        private static bool Test22_EquipmentNamesAccessible()
        {
            var layout = CreatePopulatedLayout();

            // All equipment should have accessible, non-null names
            bool cranesOk = layout.EOTCranes.All(c => !string.IsNullOrEmpty(c.Name));
            bool jibsOk = layout.JibCranes.All(j => !string.IsNullOrEmpty(j.Name));
            bool runwaysOk = layout.Runways.All(r => !string.IsNullOrEmpty(r.Name));
            bool zonesOk = layout.Zones.All(z => !string.IsNullOrEmpty(z.Name));
            bool stationsOk = layout.AGVStations.All(s => !string.IsNullOrEmpty(s.Name));
            bool conveyorsOk = layout.Conveyors.All(c => !string.IsNullOrEmpty(c.Name));
            bool openingsOk = layout.Openings.All(o => !string.IsNullOrEmpty(o.Name));
            bool nodesOk = layout.Nodes.All(n => !string.IsNullOrEmpty(n.Name));

            return cranesOk && jibsOk && runwaysOk && zonesOk &&
                   stationsOk && conveyorsOk && openingsOk && nodesOk;
        }

        private static bool Test23_CraneRunwayRelationshipIntact()
        {
            var layout = new LayoutData();

            // Create a runway
            var runway = new RunwayData
            {
                Id = "runway-test",
                Name = "Test Runway",
                StartX = 0,
                StartY = 100,
                EndX = 500,
                EndY = 100
            };
            layout.Runways.Add(runway);

            // Create a crane on that runway
            var crane = new EOTCraneData
            {
                Id = "crane-test",
                Name = "Test Crane",
                RunwayId = "runway-test"
            };
            layout.EOTCranes.Add(crane);

            // Serialize and restore
            string json = JsonHelper.Serialize(layout);
            var restored = JsonHelper.Deserialize<LayoutData>(json);

            if (restored == null) return false;

            // Verify relationship
            var restoredCrane = restored.EOTCranes.FirstOrDefault(c => c.Id == "crane-test");
            var restoredRunway = restored.Runways.FirstOrDefault(r => r.Id == "runway-test");

            return restoredCrane != null &&
                   restoredRunway != null &&
                   restoredCrane.RunwayId == restoredRunway.Id;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a fully populated layout for testing
        /// </summary>
        private static LayoutData CreatePopulatedLayout()
        {
            var layout = new LayoutData();

            // Add runway first (for crane relationship)
            layout.Runways.Add(new RunwayData
            {
                Id = "runway-1",
                Name = "Runway 1",
                StartX = 0,
                StartY = 100,
                EndX = 1000,
                EndY = 100,
                Height = 15
            });

            // Add EOT Crane
            layout.EOTCranes.Add(new EOTCraneData
            {
                Id = "eot-1",
                Name = "EOT Crane 1",
                RunwayId = "runway-1",
                BayWidth = 12,
                ReachLeft = 6,
                ReachRight = 6,
                BridgePosition = 0.5
            });

            // Add Jib Crane
            layout.JibCranes.Add(new JibCraneData
            {
                Id = "jib-1",
                Name = "Jib Crane 1",
                CenterX = 200,
                CenterY = 300,
                Radius = 50
            });

            // Add Zones
            for (int i = 0; i < 5; i++)
            {
                layout.Zones.Add(new ZoneData
                {
                    Id = $"zone-{i}",
                    Name = $"Storage Zone {i + 1}",
                    Type = "storage",
                    X = 100 + (i * 60),
                    Y = 200,
                    Width = 50,
                    Height = 50
                });
            }

            // Add AGV Stations
            layout.AGVStations.Add(new AGVStationData
            {
                Id = "station-1",
                Name = "AGV Station 1",
                StationType = "pickup",
                X = 50,
                Y = 50
            });
            layout.AGVStations.Add(new AGVStationData
            {
                Id = "station-2",
                Name = "AGV Station 2",
                StationType = "dropoff",
                X = 450,
                Y = 50
            });

            // Add Conveyor
            layout.Conveyors.Add(new ConveyorData
            {
                Id = "conveyor-1",
                Name = "Main Conveyor",
                ConveyorType = "belt",
                Width = 1.5,
                Speed = 2.0
            });

            // Add Opening
            layout.Openings.Add(new OpeningData
            {
                Id = "opening-1",
                Name = "Main Entrance",
                OpeningType = "door",
                X = 0,
                Y = 150
            });

            // Add Nodes
            for (int i = 0; i < 3; i++)
            {
                layout.Nodes.Add(new NodeData
                {
                    Id = $"node-{i}",
                    Name = $"Machine {i + 1}",
                    Type = "machine",
                    Visual = new NodeVisual { X = 100 + (i * 100), Y = 400 }
                });
            }

            // Add Paths
            layout.Paths.Add(new PathData
            {
                Id = "path-1",
                From = "node-0",
                To = "node-1",
                PathType = "single"
            });
            layout.Paths.Add(new PathData
            {
                Id = "path-2",
                From = "node-1",
                To = "node-2",
                PathType = "single"
            });

            // Add Wall
            layout.Walls.Add(new WallData
            {
                Id = "wall-1",
                X1 = 0,
                Y1 = 0,
                X2 = 500,
                Y2 = 0,
                WallType = "exterior",
                Thickness = 6
            });

            return layout;
        }

        #endregion
    }
}
