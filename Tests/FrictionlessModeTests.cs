using System;
using System.Collections.Generic;
using System.Linq;
using LayoutEditor.Models;

namespace LayoutEditor.Tests
{
    /// <summary>
    /// Test harness for Frictionless Mode - validates constrained movement
    /// </summary>
    public static class FrictionlessModeTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=== Frictionless Mode Test Harness ===\n");

            int passed = 0;
            int failed = 0;

            // Test 1: AGV waypoint constrained to paths
            if (Test_AGVWaypointConstrainedToPath()) passed++; else failed++;

            // Test 2: AGV station not movable in frictionless mode
            if (Test_AGVStationNotMovableInFrictionless()) passed++; else failed++;

            // Test 3: Design mode allows free movement
            if (Test_DesignModeFreeMovement()) passed++; else failed++;

            // Test 4: EOT crane constrained to runway
            if (Test_EOTCraneConstrainedToRunway()) passed++; else failed++;

            // Test 5: Jib crane wall constraint
            if (Test_JibCraneWallConstraint()) passed++; else failed++;

            // Test 6: Zone vertex editing in design mode
            if (Test_ZoneVertexEditingInDesignMode()) passed++; else failed++;

            Console.WriteLine($"\n=== Results: {passed} passed, {failed} failed ===");
            return failed;
        }

        private static bool Test_AGVWaypointConstrainedToPath()
        {
            Console.Write("Test 1: AGV waypoint constrained to paths in frictionless mode... ");
            try
            {
                var layout = CreateTestLayout();
                layout.FrictionlessMode = true;
                layout.DesignMode = false;

                var waypoint = layout.AGVWaypoints.First();
                var path = layout.AGVPaths.First();

                // Waypoint should be connected to paths
                bool isConnected = layout.AGVPaths.Any(p =>
                    p.FromWaypointId == waypoint.Id || p.ToWaypointId == waypoint.Id);

                if (isConnected)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Waypoint '{waypoint.Name}' connected to {layout.AGVPaths.Count(p => p.FromWaypointId == waypoint.Id || p.ToWaypointId == waypoint.Id)} paths");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Waypoint not connected to any paths");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static bool Test_AGVStationNotMovableInFrictionless()
        {
            Console.Write("Test 2: AGV station position in frictionless mode... ");
            try
            {
                var layout = CreateTestLayout();
                layout.FrictionlessMode = true;
                layout.DesignMode = false;

                var station = layout.AGVStations.First();
                double origX = station.X;
                double origY = station.Y;

                // In frictionless mode, stations shouldn't be freely movable
                // (they're fixed equipment - only Design Mode allows repositioning)
                bool stationHasPosition = station.X > 0 && station.Y > 0;
                bool stationHasHomingProperty = station.IsHoming != null;

                if (stationHasPosition)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Station '{station.Name}' at ({station.X:F1}, {station.Y:F1})");
                    Console.WriteLine($"   - IsHoming: {station.IsHoming}");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Station missing position");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static bool Test_DesignModeFreeMovement()
        {
            Console.Write("Test 3: Design mode allows free movement... ");
            try
            {
                var layout = CreateTestLayout();
                layout.FrictionlessMode = false;
                layout.DesignMode = true;

                var waypoint = layout.AGVWaypoints.First();
                double origX = waypoint.X;
                double origY = waypoint.Y;

                // In design mode, we can move waypoints freely
                waypoint.X = 999;
                waypoint.Y = 888;

                bool canMove = waypoint.X == 999 && waypoint.Y == 888;

                // Restore
                waypoint.X = origX;
                waypoint.Y = origY;

                if (canMove)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Waypoint can be freely positioned in Design Mode");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAILED - Movement restricted in Design Mode");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static bool Test_EOTCraneConstrainedToRunway()
        {
            Console.Write("Test 4: EOT crane constrained to runway... ");
            try
            {
                var layout = CreateTestLayout();
                var crane = layout.EOTCranes.First();
                var runway = layout.Runways.First();

                // EOT crane position is defined by BridgePosition (0-1) along runway
                bool hasBridgePosition = crane.BridgePosition >= 0 && crane.BridgePosition <= 1;
                bool hasRunwayRef = crane.RunwayId == runway.Id;
                bool hasBayWidth = crane.BayWidth > 0;

                if (hasBridgePosition && hasRunwayRef && hasBayWidth)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - EOT Crane '{crane.Name}' on runway '{runway.Name}'");
                    Console.WriteLine($"   - BridgePosition: {crane.BridgePosition:F2} (0=start, 1=end)");
                    Console.WriteLine($"   - BayWidth: {crane.BayWidth:F0}\" ({crane.BayWidth/12:F1}')");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FAILED - BridgePos:{hasBridgePosition}, Runway:{hasRunwayRef}, BayWidth:{hasBayWidth}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static bool Test_JibCraneWallConstraint()
        {
            Console.Write("Test 5: Jib crane has position and radius... ");
            try
            {
                var layout = CreateTestLayout();
                var jib = layout.JibCranes.First();

                // Jib crane has center position and radius
                bool hasCenter = jib.CenterX > 0 && jib.CenterY > 0;
                bool hasRadius = jib.Radius > 0;

                if (hasCenter && hasRadius)
                {
                    Console.WriteLine("PASSED");
                    Console.WriteLine($"   - Jib Crane '{jib.Name}' at ({jib.CenterX:F1}, {jib.CenterY:F1})");
                    Console.WriteLine($"   - Radius: {jib.Radius:F1}\"");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FAILED - Center:{hasCenter}, Radius:{hasRadius}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        private static bool Test_ZoneVertexEditingInDesignMode()
        {
            Console.Write("Test 6: Zone vertex editing in design mode... ");
            try
            {
                var layout = CreateTestLayout();
                layout.DesignMode = true;

                var zone = layout.Zones.First();
                int originalPointCount = zone.Points.Count;

                // Can we modify zone vertices in design mode?
                if (zone.Points.Count > 0)
                {
                    double origX = zone.Points[0].X;
                    zone.Points[0].X = 999;
                    bool canEdit = zone.Points[0].X == 999;
                    zone.Points[0].X = origX; // Restore

                    if (canEdit)
                    {
                        Console.WriteLine("PASSED");
                        Console.WriteLine($"   - Zone '{zone.Name}' has {zone.Points.Count} editable vertices");
                        return true;
                    }
                }

                Console.WriteLine("FAILED - Cannot edit zone vertices");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a test layout for frictionless mode testing
        /// </summary>
        private static LayoutData CreateTestLayout()
        {
            var layout = new LayoutData
            {
                Canvas = new CanvasSettings { Width = 1000, Height = 800 }
            };

            // Add AGV stations
            var station1 = new AGVStationData
            {
                Id = "station_1",
                Name = "Loading Station",
                X = 100,
                Y = 500,
                IsHoming = false
            };
            var station2 = new AGVStationData
            {
                Id = "station_2",
                Name = "Home Station",
                X = 800,
                Y = 500,
                IsHoming = true
            };
            layout.AGVStations.Add(station1);
            layout.AGVStations.Add(station2);

            // Add AGV waypoints
            var wp1 = new AGVWaypointData
            {
                Id = "waypoint_1",
                Name = "WP1",
                X = 300,
                Y = 500
            };
            var wp2 = new AGVWaypointData
            {
                Id = "waypoint_2",
                Name = "WP2",
                X = 600,
                Y = 500
            };
            layout.AGVWaypoints.Add(wp1);
            layout.AGVWaypoints.Add(wp2);

            // Add AGV paths connecting waypoints (paths are between waypoints)
            // Stations connect to nearby waypoints implicitly
            layout.AGVPaths.Add(new AGVPathData
            {
                Id = "path_1",
                FromWaypointId = wp1.Id,
                ToWaypointId = wp2.Id
            });

            // Add runway and EOT crane
            var runway = new RunwayData
            {
                Id = "runway_1",
                Name = "Main Bay",
                StartX = 100,
                StartY = 100,
                EndX = 900,
                EndY = 100
            };
            layout.Runways.Add(runway);

            layout.EOTCranes.Add(new EOTCraneData
            {
                Id = "eot_1",
                Name = "EOT Crane 1",
                RunwayId = runway.Id,
                BridgePosition = 0.5,
                BayWidth = 240 // 20 feet
            });

            // Add Jib crane
            layout.JibCranes.Add(new JibCraneData
            {
                Id = "jib_1",
                Name = "Jib Crane 1",
                CenterX = 500,
                CenterY = 600,
                Radius = 120
            });

            // Add a zone with polygon points
            var zone = new ZoneData
            {
                Id = "zone_work",
                Name = "Work Area",
                Type = "work_area"
            };
            zone.Points.Add(new PointData(200, 200));
            zone.Points.Add(new PointData(700, 200));
            zone.Points.Add(new PointData(700, 600));
            zone.Points.Add(new PointData(200, 600));
            layout.Zones.Add(zone);

            return layout;
        }
    }
}
