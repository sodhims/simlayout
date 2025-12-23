-- ============================================================================
-- Layout Editor PostgreSQL Schema
-- Matches actual C# model structure
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- LAYOUTS
-- ============================================================================
CREATE TABLE layouts (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    canvas_width DOUBLE PRECISION DEFAULT 1200,
    canvas_height DOUBLE PRECISION DEFAULT 800,
    grid_size INTEGER DEFAULT 20,
    snap_to_grid BOOLEAN DEFAULT true,
    show_grid BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- NODES
-- ============================================================================
CREATE TABLE nodes (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    
    -- NodeData core
    name VARCHAR(255) NOT NULL,
    node_type VARCHAR(50) NOT NULL,
    label VARCHAR(255) DEFAULT '',
    template_id VARCHAR(255),
    index_in_cell INTEGER DEFAULT -1,
    
    -- NodeVisual
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    width DOUBLE PRECISION DEFAULT 80,
    height DOUBLE PRECISION DEFAULT 60,
    rotation DOUBLE PRECISION DEFAULT 0,
    icon VARCHAR(100) DEFAULT 'machine_generic',
    color VARCHAR(20) DEFAULT '#4A90D9',
    label_position VARCHAR(20) DEFAULT 'bottom',
    label_visible BOOLEAN DEFAULT true,
    queue_direction VARCHAR(20) DEFAULT 'horizontal',
    entity_spacing DOUBLE PRECISION DEFAULT 12,
    server_arrangement VARCHAR(20) DEFAULT 'horizontal',
    server_spacing DOUBLE PRECISION DEFAULT 25,
    input_terminal_position VARCHAR(20) DEFAULT 'left',
    output_terminal_position VARCHAR(20) DEFAULT 'right',
    
    -- SimulationParams
    servers INTEGER DEFAULT 1,
    capacity INTEGER DEFAULT 1,
    initial_level INTEGER DEFAULT 0,
    priority INTEGER DEFAULT 1,
    process_time DOUBLE PRECISION,
    setup_time DOUBLE PRECISION,
    interarrival_time DOUBLE PRECISION,
    mtbf DOUBLE PRECISION,
    mttr DOUBLE PRECISION,
    queue_discipline VARCHAR(20) DEFAULT 'FIFO',
    blocking_mode VARCHAR(30) DEFAULT 'block_upstream',
    entity_type VARCHAR(50) DEFAULT 'part',
    batch_size INTEGER DEFAULT 1,
    max_arrivals INTEGER,
    collect_statistics BOOLEAN DEFAULT true,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_nodes_layout ON nodes(layout_id);
CREATE INDEX idx_nodes_type ON nodes(node_type);

-- ============================================================================
-- PATHS
-- ============================================================================
CREATE TABLE paths (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    from_node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    to_node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    
    from_terminal VARCHAR(20) DEFAULT 'output',
    to_terminal VARCHAR(20) DEFAULT 'input',
    path_type VARCHAR(50) DEFAULT 'single',
    routing_mode VARCHAR(50) DEFAULT 'direct',
    connection_type VARCHAR(50) DEFAULT 'partFlow',
    
    -- PathVisual
    color VARCHAR(20) DEFAULT '#888888',
    thickness DOUBLE PRECISION DEFAULT 2,
    style VARCHAR(30) DEFAULT 'solid',
    arrow_size DOUBLE PRECISION DEFAULT 8,
    lane_spacing DOUBLE PRECISION DEFAULT 6,
    
    -- PathSimulation
    distance DOUBLE PRECISION,
    transport_type VARCHAR(50) DEFAULT 'conveyor',
    speed DOUBLE PRECISION DEFAULT 1.0,
    capacity INTEGER DEFAULT 10,
    lanes INTEGER DEFAULT 1,
    bidirectional BOOLEAN DEFAULT false,
    speed_limit DOUBLE PRECISION DEFAULT 2.0,
    is_blocked BOOLEAN DEFAULT false,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_paths_layout ON paths(layout_id);
CREATE INDEX idx_paths_from ON paths(from_node_id);
CREATE INDEX idx_paths_to ON paths(to_node_id);

CREATE TABLE path_waypoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    path_id UUID NOT NULL REFERENCES paths(id) ON DELETE CASCADE,
    sequence_order INTEGER NOT NULL,
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    UNIQUE(path_id, sequence_order)
);

-- ============================================================================
-- CELLS
-- ============================================================================
CREATE TABLE cells (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    cell_index INTEGER DEFAULT 0,
    canonical_name VARCHAR(255),
    cell_type VARCHAR(50) DEFAULT 'simple',
    is_collapsed BOOLEAN DEFAULT false,
    color VARCHAR(20) DEFAULT '#9B59B6',
    input_terminal_position VARCHAR(20) DEFAULT 'left',
    output_terminal_position VARCHAR(20) DEFAULT 'right',
    assembly_mode VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_cells_layout ON cells(layout_id);

CREATE TABLE cell_members (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

CREATE TABLE cell_entry_points (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

CREATE TABLE cell_exit_points (
    cell_id UUID NOT NULL REFERENCES cells(id) ON DELETE CASCADE,
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (cell_id, node_id)
);

-- ============================================================================
-- WALLS
-- ============================================================================
CREATE TABLE walls (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    x1 DOUBLE PRECISION NOT NULL,
    y1 DOUBLE PRECISION NOT NULL,
    x2 DOUBLE PRECISION NOT NULL,
    y2 DOUBLE PRECISION NOT NULL,
    thickness DOUBLE PRECISION DEFAULT 6,
    wall_type VARCHAR(50) DEFAULT 'standard',
    color VARCHAR(20) DEFAULT '#444444',
    dash_pattern VARCHAR(50) DEFAULT '',
    line_style VARCHAR(20) DEFAULT 'solid',
    layer VARCHAR(255) DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_walls_layout ON walls(layout_id);

-- ============================================================================
-- TRANSPORT NETWORKS
-- ============================================================================
CREATE TABLE transport_networks (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT DEFAULT '',
    is_visible BOOLEAN DEFAULT true,
    is_locked BOOLEAN DEFAULT false,
    color VARCHAR(20) DEFAULT '#E67E22',
    track_width DOUBLE PRECISION DEFAULT 30,
    track_style VARCHAR(20) DEFAULT 'double',
    network_type VARCHAR(50) DEFAULT 'agv',
    bidirectional BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transport_networks_layout ON transport_networks(layout_id);

CREATE TABLE transport_stations (
    id UUID PRIMARY KEY,
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    label VARCHAR(255) DEFAULT '',
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    width DOUBLE PRECISION DEFAULT 50,
    height DOUBLE PRECISION DEFAULT 50,
    rotation DOUBLE PRECISION DEFAULT 0,
    color VARCHAR(20) DEFAULT '#9B59B6',
    icon VARCHAR(100) DEFAULT 'default',
    size DOUBLE PRECISION DEFAULT 50,
    station_type VARCHAR(50) DEFAULT 'pickup',
    queue_capacity INTEGER DEFAULT 5,
    dwell_time DOUBLE PRECISION DEFAULT 10,
    is_blocking BOOLEAN DEFAULT false,
    load_time DOUBLE PRECISION DEFAULT 5,
    unload_time DOUBLE PRECISION DEFAULT 5,
    priority INTEGER DEFAULT 0,
    capacity INTEGER DEFAULT 5,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transport_stations_network ON transport_stations(network_id);
CREATE INDEX idx_transport_stations_layout ON transport_stations(layout_id);

CREATE TABLE transport_tracks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    from_point_id UUID NOT NULL REFERENCES transport_stations(id) ON DELETE CASCADE,
    to_point_id UUID NOT NULL REFERENCES transport_stations(id) ON DELETE CASCADE,
    distance DOUBLE PRECISION DEFAULT 0,
    is_bidirectional BOOLEAN DEFAULT true,
    speed_limit DOUBLE PRECISION DEFAULT 2.0,
    lane_count INTEGER DEFAULT 1,
    color VARCHAR(20) DEFAULT '',
    priority INTEGER DEFAULT 0,
    is_blocked BOOLEAN DEFAULT false,
    weight DOUBLE PRECISION DEFAULT 1.0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transport_tracks_network ON transport_tracks(network_id);

CREATE TABLE transporters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES transport_networks(id) ON DELETE CASCADE,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    transporter_type VARCHAR(50) DEFAULT 'agv',
    home_station_id UUID REFERENCES transport_stations(id),
    speed DOUBLE PRECISION DEFAULT 1.5,
    acceleration DOUBLE PRECISION DEFAULT 0.5,
    load_capacity INTEGER DEFAULT 1,
    color VARCHAR(20) DEFAULT '#E74C3C',
    length DOUBLE PRECISION DEFAULT 1.0,
    width DOUBLE PRECISION DEFAULT 0.8,
    battery_capacity DOUBLE PRECISION DEFAULT 100,
    battery_consumption DOUBLE PRECISION DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transporters_network ON transporters(network_id);

-- ============================================================================
-- VIEWS
-- ============================================================================
CREATE OR REPLACE VIEW v_connections AS
SELECT p.id, p.layout_id, fn.name as from_name, tn.name as to_name,
       p.connection_type, p.distance, p.speed, p.capacity
FROM paths p
JOIN nodes fn ON p.from_node_id = fn.id
JOIN nodes tn ON p.to_node_id = tn.id;

CREATE OR REPLACE VIEW v_nodes_with_cells AS
SELECT n.*, c.name as cell_name, c.cell_type
FROM nodes n
LEFT JOIN cell_members cm ON n.id = cm.node_id
LEFT JOIN cells c ON cm.cell_id = c.id;

-- ============================================================================
-- TRIGGERS
-- ============================================================================
CREATE OR REPLACE FUNCTION update_layout_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE layouts SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = COALESCE(NEW.layout_id, OLD.layout_id);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER nodes_update_layout AFTER INSERT OR UPDATE OR DELETE ON nodes
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER paths_update_layout AFTER INSERT OR UPDATE OR DELETE ON paths
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER cells_update_layout AFTER INSERT OR UPDATE OR DELETE ON cells
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER walls_update_layout AFTER INSERT OR UPDATE OR DELETE ON walls
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();

-- ============================================================================
-- RUNWAYS (Infrastructure for EOT Cranes)
-- ============================================================================
CREATE TABLE runways (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Geometry
    start_x DOUBLE PRECISION NOT NULL,
    start_y DOUBLE PRECISION NOT NULL,
    end_x DOUBLE PRECISION NOT NULL,
    end_y DOUBLE PRECISION NOT NULL,
    height DOUBLE PRECISION DEFAULT 20,          -- Height above floor (meters)

    -- Specifications
    capacity_kg DOUBLE PRECISION DEFAULT 5000,   -- Load capacity in kg
    runway_pair_id UUID REFERENCES runways(id),  -- Paired parallel runway
    color VARCHAR(20) DEFAULT '#666666',

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_runways_layout ON runways(layout_id);

-- ============================================================================
-- EOT CRANES (Electric Overhead Traveling Cranes)
-- ============================================================================
CREATE TABLE eot_cranes (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    runway_id UUID NOT NULL REFERENCES runways(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Bay/Reach dimensions (perpendicular to runway)
    bay_width DOUBLE PRECISION DEFAULT 240,      -- Total bay width (inches)
    reach_left DOUBLE PRECISION DEFAULT 120,     -- Reach to left of runway
    reach_right DOUBLE PRECISION DEFAULT 120,    -- Reach to right of runway

    -- Zone constraints (fraction of runway 0.0-1.0)
    zone_min DOUBLE PRECISION DEFAULT 0,         -- Start of travel zone
    zone_max DOUBLE PRECISION DEFAULT 1,         -- End of travel zone
    bridge_position DOUBLE PRECISION DEFAULT 0.5, -- Current position on runway

    -- Speeds (meters/second)
    speed_bridge DOUBLE PRECISION DEFAULT 1.0,   -- Bridge travel speed
    speed_trolley DOUBLE PRECISION DEFAULT 0.5,  -- Trolley cross-travel speed
    speed_hoist DOUBLE PRECISION DEFAULT 0.3,    -- Hoist vertical speed

    -- Load specifications
    max_load_kg DOUBLE PRECISION DEFAULT 5000,   -- Maximum lift capacity
    hook_height DOUBLE PRECISION DEFAULT 10,     -- Hook height range

    color VARCHAR(20) DEFAULT '#E67E22',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_eot_cranes_layout ON eot_cranes(layout_id);
CREATE INDEX idx_eot_cranes_runway ON eot_cranes(runway_id);

-- ============================================================================
-- JIB CRANES (Fixed pivot point with rotating arm)
-- ============================================================================
CREATE TABLE jib_cranes (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Pivot location
    center_x DOUBLE PRECISION NOT NULL,
    center_y DOUBLE PRECISION NOT NULL,

    -- Arm specifications
    radius DOUBLE PRECISION DEFAULT 20,          -- Arm reach length
    arc_start DOUBLE PRECISION DEFAULT 0,        -- Start angle (degrees)
    arc_end DOUBLE PRECISION DEFAULT 360,        -- End angle (degrees)

    -- Speeds
    speed_slew DOUBLE PRECISION DEFAULT 10,      -- Rotation speed (deg/sec)
    speed_hoist DOUBLE PRECISION DEFAULT 0.3,    -- Hoist speed (m/s)

    -- Load specifications
    max_load_kg DOUBLE PRECISION DEFAULT 1000,   -- Maximum lift capacity

    color VARCHAR(20) DEFAULT '#27AE60',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_jib_cranes_layout ON jib_cranes(layout_id);

-- ============================================================================
-- HANDOFF POINTS (Transfer points between cranes)
-- ============================================================================
CREATE TABLE handoff_points (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    runway_id UUID NOT NULL REFERENCES runways(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Location
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    position DOUBLE PRECISION DEFAULT 0.5,       -- Position along runway (0-1)

    -- Connected cranes
    crane1_id UUID REFERENCES eot_cranes(id),
    crane2_id UUID REFERENCES eot_cranes(id),

    -- Handoff configuration
    handoff_type VARCHAR(50) DEFAULT 'direct',   -- direct, ground_buffer
    handoff_rule VARCHAR(50) DEFAULT 'transfer', -- transfer, clearAndPickup

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_handoff_points_layout ON handoff_points(layout_id);

-- ============================================================================
-- ZONES (Storage, Work, Restricted areas)
-- ============================================================================
CREATE TABLE zones (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Zone type and purpose
    zone_type VARCHAR(50) DEFAULT 'storage',     -- storage, work, staging, restricted, shipping, receiving

    -- Capacity and dimensions
    capacity INTEGER DEFAULT 100,                -- Storage capacity (units)
    area_sqft DOUBLE PRECISION,                  -- Calculated area

    -- Rectangle bounds (for simple zones)
    x DOUBLE PRECISION DEFAULT 0,
    y DOUBLE PRECISION DEFAULT 0,
    width DOUBLE PRECISION DEFAULT 100,
    height DOUBLE PRECISION DEFAULT 100,

    -- Visual
    fill_color VARCHAR(20) DEFAULT '#3498DB',
    border_color VARCHAR(20) DEFAULT '#2980B9',
    opacity DOUBLE PRECISION DEFAULT 0.3,

    -- Safety/Access
    is_restricted BOOLEAN DEFAULT false,
    required_ppe VARCHAR(255) DEFAULT '',        -- Required PPE list
    max_occupancy INTEGER DEFAULT 0,             -- 0 = unlimited

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_zones_layout ON zones(layout_id);
CREATE INDEX idx_zones_type ON zones(zone_type);

-- Zone polygon points (for non-rectangular zones)
CREATE TABLE zone_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    zone_id UUID NOT NULL REFERENCES zones(id) ON DELETE CASCADE,
    sequence_order INTEGER NOT NULL,
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    UNIQUE(zone_id, sequence_order)
);

-- ============================================================================
-- CONVEYORS
-- ============================================================================
CREATE TABLE conveyors (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Type and configuration
    conveyor_type VARCHAR(50) DEFAULT 'belt',    -- belt, roller, chain, overhead, gravity
    direction VARCHAR(20) DEFAULT 'forward',     -- forward, reverse, bidirectional
    is_accumulating BOOLEAN DEFAULT false,       -- Can items queue on conveyor

    -- Dimensions
    width DOUBLE PRECISION DEFAULT 24,           -- Width in inches
    length DOUBLE PRECISION,                     -- Calculated from path

    -- Speed and capacity
    speed DOUBLE PRECISION DEFAULT 60,           -- Speed (ft/min)
    capacity INTEGER DEFAULT 10,                 -- Max items on conveyor

    -- Connected equipment
    from_node_id UUID REFERENCES nodes(id),
    to_node_id UUID REFERENCES nodes(id),

    color VARCHAR(20) DEFAULT '#95A5A6',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_conveyors_layout ON conveyors(layout_id);

-- Conveyor path points (centerline)
CREATE TABLE conveyor_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conveyor_id UUID NOT NULL REFERENCES conveyors(id) ON DELETE CASCADE,
    sequence_order INTEGER NOT NULL,
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    UNIQUE(conveyor_id, sequence_order)
);

-- ============================================================================
-- STORAGE AREAS (Detailed storage locations)
-- ============================================================================
CREATE TABLE storage_areas (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    zone_id UUID REFERENCES zones(id),           -- Parent zone if any
    name VARCHAR(255) NOT NULL,

    -- Location
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    width DOUBLE PRECISION DEFAULT 48,
    height DOUBLE PRECISION DEFAULT 48,
    rotation DOUBLE PRECISION DEFAULT 0,

    -- Storage type
    storage_type VARCHAR(50) DEFAULT 'floor',    -- floor, rack, shelf, bin, pallet
    levels INTEGER DEFAULT 1,                    -- Number of vertical levels
    positions_per_level INTEGER DEFAULT 1,       -- Positions per level

    -- Capacity
    capacity INTEGER DEFAULT 1,                  -- Total capacity
    current_level INTEGER DEFAULT 0,             -- Current occupancy

    -- Access
    access_side VARCHAR(20) DEFAULT 'front',     -- front, back, both, all
    aisle_id UUID,                               -- Associated aisle

    -- Material handling
    requires_forklift BOOLEAN DEFAULT false,
    max_weight_kg DOUBLE PRECISION DEFAULT 1000,

    color VARCHAR(20) DEFAULT '#8E44AD',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_storage_areas_layout ON storage_areas(layout_id);
CREATE INDEX idx_storage_areas_zone ON storage_areas(zone_id);

-- ============================================================================
-- AGV STATIONS (Enhanced)
-- ============================================================================
CREATE TABLE agv_stations (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Location
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    rotation DOUBLE PRECISION DEFAULT 0,

    -- Station type and behavior
    station_type VARCHAR(50) DEFAULT 'pickup',   -- pickup, dropoff, dual, charging, parking

    -- Linked equipment
    linked_equipment_id UUID,                    -- Machine/buffer this serves
    linked_waypoint_id UUID,                     -- AGV network waypoint

    -- Docking specifications
    docking_tolerance DOUBLE PRECISION DEFAULT 2, -- Position tolerance (inches)
    approach_angle DOUBLE PRECISION DEFAULT 0,   -- Required approach angle

    -- Timing
    service_time DOUBLE PRECISION DEFAULT 30,    -- Load/unload time (seconds)
    dwell_time DOUBLE PRECISION DEFAULT 5,       -- Minimum wait time

    -- Queue
    queue_capacity INTEGER DEFAULT 3,            -- Max AGVs waiting
    is_blocking BOOLEAN DEFAULT false,           -- Block if full

    -- Home station
    is_homing BOOLEAN DEFAULT false,             -- AGVs return here when idle
    priority INTEGER DEFAULT 0,                  -- Station priority

    color VARCHAR(20) DEFAULT '#9B59B6',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_agv_stations_layout ON agv_stations(layout_id);

-- ============================================================================
-- OPENINGS (Doors, Gates, Hatches, Aisles, etc.)
-- ============================================================================
CREATE TABLE openings (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Location and orientation
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    rotation DOUBLE PRECISION DEFAULT 0,            -- Degrees

    -- Opening type and state
    opening_type VARCHAR(50) DEFAULT 'door',        -- door, gate, hatch, manhole, aisle, bay_entrance, emergency_exit
    state VARCHAR(50) DEFAULT 'open',               -- open, closed, locked, emergency

    -- Physical dimensions
    clear_width DOUBLE PRECISION DEFAULT 36,        -- Clear width (inches)
    clear_height DOUBLE PRECISION DEFAULT 80,       -- Clear height (inches)
    max_load_weight DOUBLE PRECISION DEFAULT 0,     -- Max load weight (kg), 0 = no limit

    -- Capacity (0 = unconstrained, N = requires token)
    capacity INTEGER DEFAULT 1,

    -- Flow control
    direction_mode VARCHAR(50) DEFAULT 'bidirectional', -- bidirectional, inbound_only, outbound_only
    traversal_time DOUBLE PRECISION DEFAULT 2.0,    -- Time to pass through (seconds)

    -- Zone connections
    from_zone_id UUID REFERENCES zones(id),
    to_zone_id UUID REFERENCES zones(id),

    -- Door-specific properties
    swing_direction VARCHAR(50),                    -- inward, outward, sliding, bidirectional
    swing_angle DOUBLE PRECISION DEFAULT 90,        -- Swing arc in degrees
    auto_close BOOLEAN DEFAULT false,
    access_control VARCHAR(255) DEFAULT '',         -- Access control system ID

    -- Gate-specific properties
    vehicle_only BOOLEAN DEFAULT false,
    barrier_type VARCHAR(50) DEFAULT '',            -- sliding, swing, lift

    -- Hatch-specific properties
    is_vertical BOOLEAN DEFAULT false,
    ladder_time DOUBLE PRECISION DEFAULT 5.0,       -- Additional ladder traverse time

    -- Manhole-specific properties
    confined_space_protocol BOOLEAN DEFAULT false,
    requires_permit BOOLEAN DEFAULT false,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_openings_layout ON openings(layout_id);
CREATE INDEX idx_openings_type ON openings(opening_type);
CREATE INDEX idx_openings_from_zone ON openings(from_zone_id);
CREATE INDEX idx_openings_to_zone ON openings(to_zone_id);

-- ============================================================================
-- TRAFFIC ZONES (AGV traffic control)
-- ============================================================================
CREATE TABLE traffic_zones (
    id UUID PRIMARY KEY,
    layout_id UUID NOT NULL REFERENCES layouts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,

    -- Zone type
    zone_type VARCHAR(50) DEFAULT 'intersection', -- intersection, corridor, staging

    -- Capacity
    max_vehicles INTEGER DEFAULT 1,              -- Max simultaneous AGVs

    -- Priority and control
    priority INTEGER DEFAULT 0,
    control_type VARCHAR(50) DEFAULT 'fifo',     -- fifo, priority, alternating

    color VARCHAR(20) DEFAULT '#F39C12',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_traffic_zones_layout ON traffic_zones(layout_id);

-- Traffic zone boundary points
CREATE TABLE traffic_zone_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    traffic_zone_id UUID NOT NULL REFERENCES traffic_zones(id) ON DELETE CASCADE,
    sequence_order INTEGER NOT NULL,
    x DOUBLE PRECISION NOT NULL,
    y DOUBLE PRECISION NOT NULL,
    UNIQUE(traffic_zone_id, sequence_order)
);

-- ============================================================================
-- ADDITIONAL TRIGGERS
-- ============================================================================
CREATE TRIGGER runways_update_layout AFTER INSERT OR UPDATE OR DELETE ON runways
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER eot_cranes_update_layout AFTER INSERT OR UPDATE OR DELETE ON eot_cranes
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER jib_cranes_update_layout AFTER INSERT OR UPDATE OR DELETE ON jib_cranes
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER zones_update_layout AFTER INSERT OR UPDATE OR DELETE ON zones
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER conveyors_update_layout AFTER INSERT OR UPDATE OR DELETE ON conveyors
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER storage_areas_update_layout AFTER INSERT OR UPDATE OR DELETE ON storage_areas
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER agv_stations_update_layout AFTER INSERT OR UPDATE OR DELETE ON agv_stations
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();
CREATE TRIGGER openings_update_layout AFTER INSERT OR UPDATE OR DELETE ON openings
    FOR EACH ROW EXECUTE FUNCTION update_layout_timestamp();

-- ============================================================================
-- EQUIPMENT SUMMARY VIEW
-- ============================================================================
CREATE OR REPLACE VIEW v_equipment_summary AS
SELECT
    l.id as layout_id,
    l.name as layout_name,
    (SELECT COUNT(*) FROM eot_cranes WHERE layout_id = l.id) as eot_crane_count,
    (SELECT COUNT(*) FROM jib_cranes WHERE layout_id = l.id) as jib_crane_count,
    (SELECT COUNT(*) FROM zones WHERE layout_id = l.id) as zone_count,
    (SELECT COUNT(*) FROM conveyors WHERE layout_id = l.id) as conveyor_count,
    (SELECT COUNT(*) FROM storage_areas WHERE layout_id = l.id) as storage_area_count,
    (SELECT COUNT(*) FROM agv_stations WHERE layout_id = l.id) as agv_station_count,
    (SELECT COUNT(*) FROM openings WHERE layout_id = l.id) as opening_count,
    (SELECT COUNT(*) FROM nodes WHERE layout_id = l.id) as node_count
FROM layouts l;
