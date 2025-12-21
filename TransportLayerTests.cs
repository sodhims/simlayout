using System;
using System.Linq;
using LayoutEditor.Models;
using LayoutEditor.Services;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Manual test verification for Week 1 Layer Infrastructure implementation.
    /// Run these tests to verify the completion criteria are met.
    /// </summary>
    public static class TransportLayerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Transport Layer Infrastructure Tests ===\n");

            int passed = 0;
            int failed = 0;

            // T1.1: Layer enum has 8 values
            if (Test_T1_1_EnumHas8Values())
                passed++;
            else
                failed++;

            // T1.2: Infrastructure is value 0
            if (Test_T1_2_InfrastructureIsZero())
                passed++;
            else
                failed++;

            // T1.3: Pedestrian is value 7
            if (Test_T1_3_PedestrianIsSeven())
                passed++;
            else
                failed++;

            // T1.4: All layers visible by default
            if (Test_T1_4_AllLayersVisibleByDefault())
                passed++;
            else
                failed++;

            // T1.5: Active layer default is Equipment
            if (Test_T1_5_DefaultActiveLayerIsEquipment())
                passed++;
            else
                failed++;

            // T1.6: Visibility change fires event
            if (Test_T1_6_VisibilityChangeFiresEvent())
                passed++;
            else
                failed++;

            // T1.7: Locked layer not editable
            if (Test_T1_7_LockedLayerNotEditable())
                passed++;
            else
                failed++;

            // T1.8: Model classes have TransportLayer property
            if (Test_T1_8_ModelsHaveLayerProperty())
                passed++;
            else
                failed++;

            // T1.9: Metadata provides correct information
            if (Test_T1_9_MetadataIsCorrect())
                passed++;
            else
                failed++;

            Console.WriteLine($"\n=== Test Results ===");
            Console.WriteLine($"Passed: {passed}/9");
            Console.WriteLine($"Failed: {failed}/9");
            Console.WriteLine($"Status: {(failed == 0 ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED")}");
        }

        private static bool Test_T1_1_EnumHas8Values()
        {
            var count = Enum.GetValues(typeof(LayerType)).Length;
            var result = count == 8;
            Console.WriteLine($"T1.1 - Layer enum has 8 values: {(result ? "✓ PASS" : "✗ FAIL")} (Count: {count})");
            return result;
        }

        private static bool Test_T1_2_InfrastructureIsZero()
        {
            var value = (int)LayerType.Infrastructure;
            var result = value == 0;
            Console.WriteLine($"T1.2 - Infrastructure is value 0: {(result ? "✓ PASS" : "✗ FAIL")} (Value: {value})");
            return result;
        }

        private static bool Test_T1_3_PedestrianIsSeven()
        {
            var value = (int)LayerType.Pedestrian;
            var result = value == 7;
            Console.WriteLine($"T1.3 - Pedestrian is value 7: {(result ? "✓ PASS" : "✗ FAIL")} (Value: {value})");
            return result;
        }

        private static bool Test_T1_4_AllLayersVisibleByDefault()
        {
            var manager = new ArchitectureLayerManager();
            var allVisible = LayerMetadata.AllLayers.All(m => manager.IsVisible(m.Layer));
            Console.WriteLine($"T1.4 - All layers visible by default: {(allVisible ? "✓ PASS" : "✗ FAIL")}");
            return allVisible;
        }

        private static bool Test_T1_5_DefaultActiveLayerIsEquipment()
        {
            var manager = new ArchitectureLayerManager();
            var result = manager.ActiveLayer == LayerType.Equipment;
            Console.WriteLine($"T1.5 - Active layer default is Equipment: {(result ? "✓ PASS" : "✗ FAIL")} (Active: {manager.ActiveLayer})");
            return result;
        }

        private static bool Test_T1_6_VisibilityChangeFiresEvent()
        {
            var manager = new ArchitectureLayerManager();
            bool eventFired = false;

            manager.VisibilityChanged += (s, e) => { eventFired = true; };
            manager.SetVisibility(LayerType.Infrastructure, false);

            Console.WriteLine($"T1.6 - Visibility change fires event: {(eventFired ? "✓ PASS" : "✗ FAIL")}");
            return eventFired;
        }

        private static bool Test_T1_7_LockedLayerNotEditable()
        {
            var manager = new ArchitectureLayerManager();
            manager.SetLocked(LayerType.Infrastructure, true);
            var result = !manager.IsEditable(LayerType.Infrastructure);
            Console.WriteLine($"T1.7 - Locked layer not editable: {(result ? "✓ PASS" : "✗ FAIL")}");
            return result;
        }

        private static bool Test_T1_8_ModelsHaveLayerProperty()
        {
            try
            {
                var wall = new WallData();
                var column = new ColumnData();
                var zone = new ZoneData();
                var node = new NodeData();
                var path = new PathData();

                var wallLayer = wall.ArchitectureLayer;
                var columnLayer = column.ArchitectureLayer;
                var zoneLayer = zone.ArchitectureLayer;
                var nodeLayer = node.ArchitectureLayer;
                var pathLayer = path.ArchitectureLayer;

                bool wallCorrect = wallLayer == LayerType.Infrastructure;
                bool columnCorrect = columnLayer == LayerType.Infrastructure;
                bool zoneCorrect = zoneLayer == LayerType.Spatial;
                bool nodeCorrect = nodeLayer == LayerType.Equipment;
                bool pathCorrect = pathLayer == LayerType.LocalFlow;

                bool result = wallCorrect && columnCorrect && zoneCorrect && nodeCorrect && pathCorrect;

                Console.WriteLine($"T1.8 - Model classes have TransportLayer property: {(result ? "✓ PASS" : "✗ FAIL")}");
                if (!result)
                {
                    Console.WriteLine($"  Wall: {(wallCorrect ? "✓" : "✗")} (Expected: Infrastructure, Got: {wallLayer})");
                    Console.WriteLine($"  Column: {(columnCorrect ? "✓" : "✗")} (Expected: Infrastructure, Got: {columnLayer})");
                    Console.WriteLine($"  Zone: {(zoneCorrect ? "✓" : "✗")} (Expected: Spatial, Got: {zoneLayer})");
                    Console.WriteLine($"  Node: {(nodeCorrect ? "✓" : "✗")} (Expected: Equipment, Got: {nodeLayer})");
                    Console.WriteLine($"  Path: {(pathCorrect ? "✓" : "✗")} (Expected: LocalFlow, Got: {pathLayer})");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T1.8 - Model classes have TransportLayer property: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }

        private static bool Test_T1_9_MetadataIsCorrect()
        {
            try
            {
                var metadata = LayerMetadata.AllLayers;

                // Check count
                if (metadata.Length != 8)
                {
                    Console.WriteLine($"T1.9 - Metadata is correct: ✗ FAIL (Expected 8 layers, got {metadata.Length})");
                    return false;
                }

                // Check Z-order increases with layer number
                bool zOrderIncreases = true;
                for (int i = 0; i < metadata.Length - 1; i++)
                {
                    if (metadata[i].ZOrderBase >= metadata[i + 1].ZOrderBase)
                    {
                        zOrderIncreases = false;
                        break;
                    }
                }

                // Check specific metadata values
                var infraMetadata = LayerMetadata.GetMetadata(LayerType.Infrastructure);
                var equipMetadata = LayerMetadata.GetMetadata(LayerType.Equipment);
                var pedMetadata = LayerMetadata.GetMetadata(LayerType.Pedestrian);

                bool hasNames = !string.IsNullOrEmpty(infraMetadata.Name) &&
                               !string.IsNullOrEmpty(equipMetadata.Name) &&
                               !string.IsNullOrEmpty(pedMetadata.Name);

                bool hasColors = !string.IsNullOrEmpty(infraMetadata.DefaultColor) &&
                                !string.IsNullOrEmpty(equipMetadata.DefaultColor) &&
                                !string.IsNullOrEmpty(pedMetadata.DefaultColor);

                bool result = zOrderIncreases && hasNames && hasColors;

                Console.WriteLine($"T1.9 - Metadata is correct: {(result ? "✓ PASS" : "✗ FAIL")}");
                if (!result)
                {
                    Console.WriteLine($"  Z-order increases: {(zOrderIncreases ? "✓" : "✗")}");
                    Console.WriteLine($"  Has names: {(hasNames ? "✓" : "✗")}");
                    Console.WriteLine($"  Has colors: {(hasColors ? "✓" : "✗")}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"T1.9 - Metadata is correct: ✗ FAIL (Exception: {ex.Message})");
                return false;
            }
        }
    }
}
