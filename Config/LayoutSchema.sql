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
