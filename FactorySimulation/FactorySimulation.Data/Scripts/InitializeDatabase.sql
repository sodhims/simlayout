-- ============================================================================
-- Factory Simulation SQLite Schema
-- Based on Layout Editor schema, adapted for SQLite
-- ============================================================================

-- ============================================================================
-- SCENARIOS (New for Factory Configurator)
-- ============================================================================
CREATE TABLE IF NOT EXISTS Scenarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT UNIQUE NOT NULL,
    Description TEXT,
    ParentScenarioId INTEGER REFERENCES Scenarios(Id),
    IsBase INTEGER DEFAULT 0,
    CreatedAt TEXT DEFAULT (datetime('now')),
    ModifiedAt TEXT DEFAULT (datetime('now'))
);

-- Insert base scenario if not exists
INSERT OR IGNORE INTO Scenarios (Id, Name, Description, IsBase, CreatedAt, ModifiedAt)
VALUES (1, 'Base', 'Base configuration scenario', 1, datetime('now'), datetime('now'));

-- ============================================================================
-- LAYOUTS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Layouts (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    CanvasWidth REAL DEFAULT 1200,
    CanvasHeight REAL DEFAULT 800,
    GridSize INTEGER DEFAULT 20,
    SnapToGrid INTEGER DEFAULT 1,
    ShowGrid INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

-- ============================================================================
-- ELEMENTS (Generic element storage)
-- ============================================================================
CREATE TABLE IF NOT EXISTS Elements (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    ElementType TEXT NOT NULL,
    Name TEXT NOT NULL,
    X REAL DEFAULT 0,
    Y REAL DEFAULT 0,
    Width REAL DEFAULT 80,
    Height REAL DEFAULT 60,
    Rotation REAL DEFAULT 0,
    Properties TEXT,  -- JSON blob for type-specific properties
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_elements_layout ON Elements(LayoutId);
CREATE INDEX IF NOT EXISTS idx_elements_type ON Elements(ElementType);

-- ============================================================================
-- CONNECTIONS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Connections (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    FromElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
    ToElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
    ConnectionType TEXT DEFAULT 'flow',
    Properties TEXT,  -- JSON blob
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_connections_layout ON Connections(LayoutId);
CREATE INDEX IF NOT EXISTS idx_connections_from ON Connections(FromElementId);
CREATE INDEX IF NOT EXISTS idx_connections_to ON Connections(ToElementId);

-- ============================================================================
-- ZONES
-- ============================================================================
CREATE TABLE IF NOT EXISTS Zones (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    ZoneType TEXT DEFAULT 'storage',
    X REAL DEFAULT 0,
    Y REAL DEFAULT 0,
    Width REAL DEFAULT 100,
    Height REAL DEFAULT 100,
    Capacity INTEGER DEFAULT 100,
    FillColor TEXT DEFAULT '#3498DB',
    BorderColor TEXT DEFAULT '#2980B9',
    Opacity REAL DEFAULT 0.3,
    Properties TEXT,  -- JSON blob
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_zones_layout ON Zones(LayoutId);
CREATE INDEX IF NOT EXISTS idx_zones_type ON Zones(ZoneType);

-- ============================================================================
-- ELEMENT ZONES (Many-to-many relationship)
-- ============================================================================
CREATE TABLE IF NOT EXISTS ElementZones (
    ElementId TEXT NOT NULL REFERENCES Elements(Id) ON DELETE CASCADE,
    ZoneId TEXT NOT NULL REFERENCES Zones(Id) ON DELETE CASCADE,
    PRIMARY KEY (ElementId, ZoneId)
);

-- ============================================================================
-- NODES (Machines, buffers, sources, sinks)
-- ============================================================================
CREATE TABLE IF NOT EXISTS Nodes (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    NodeType TEXT NOT NULL,
    Label TEXT DEFAULT '',
    X REAL NOT NULL,
    Y REAL NOT NULL,
    Width REAL DEFAULT 80,
    Height REAL DEFAULT 60,
    Rotation REAL DEFAULT 0,
    Icon TEXT DEFAULT 'machine_generic',
    Color TEXT DEFAULT '#4A90D9',
    Servers INTEGER DEFAULT 1,
    Capacity INTEGER DEFAULT 1,
    ProcessTime REAL,
    SetupTime REAL,
    Properties TEXT,  -- JSON blob for additional properties
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_nodes_layout ON Nodes(LayoutId);
CREATE INDEX IF NOT EXISTS idx_nodes_type ON Nodes(NodeType);

-- ============================================================================
-- PATHS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Paths (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    FromNodeId TEXT NOT NULL REFERENCES Nodes(Id) ON DELETE CASCADE,
    ToNodeId TEXT NOT NULL REFERENCES Nodes(Id) ON DELETE CASCADE,
    PathType TEXT DEFAULT 'single',
    ConnectionType TEXT DEFAULT 'partFlow',
    Distance REAL,
    Speed REAL DEFAULT 1.0,
    Capacity INTEGER DEFAULT 10,
    Properties TEXT,  -- JSON blob
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_paths_layout ON Paths(LayoutId);
CREATE INDEX IF NOT EXISTS idx_paths_from ON Paths(FromNodeId);
CREATE INDEX IF NOT EXISTS idx_paths_to ON Paths(ToNodeId);

-- ============================================================================
-- RUNWAYS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Runways (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    StartX REAL NOT NULL,
    StartY REAL NOT NULL,
    EndX REAL NOT NULL,
    EndY REAL NOT NULL,
    Height REAL DEFAULT 20,
    CapacityKg REAL DEFAULT 5000,
    Color TEXT DEFAULT '#666666',
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_runways_layout ON Runways(LayoutId);

-- ============================================================================
-- EOT CRANES
-- ============================================================================
CREATE TABLE IF NOT EXISTS EOTCranes (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    RunwayId TEXT NOT NULL REFERENCES Runways(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    BayWidth REAL DEFAULT 240,
    ReachLeft REAL DEFAULT 120,
    ReachRight REAL DEFAULT 120,
    ZoneMin REAL DEFAULT 0,
    ZoneMax REAL DEFAULT 1,
    BridgePosition REAL DEFAULT 0.5,
    SpeedBridge REAL DEFAULT 1.0,
    SpeedTrolley REAL DEFAULT 0.5,
    SpeedHoist REAL DEFAULT 0.3,
    MaxLoadKg REAL DEFAULT 5000,
    Color TEXT DEFAULT '#E67E22',
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_eot_cranes_layout ON EOTCranes(LayoutId);
CREATE INDEX IF NOT EXISTS idx_eot_cranes_runway ON EOTCranes(RunwayId);

-- ============================================================================
-- JIB CRANES
-- ============================================================================
CREATE TABLE IF NOT EXISTS JibCranes (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    CenterX REAL NOT NULL,
    CenterY REAL NOT NULL,
    Radius REAL DEFAULT 20,
    ArcStart REAL DEFAULT 0,
    ArcEnd REAL DEFAULT 360,
    SpeedSlew REAL DEFAULT 10,
    SpeedHoist REAL DEFAULT 0.3,
    MaxLoadKg REAL DEFAULT 1000,
    Color TEXT DEFAULT '#27AE60',
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_jib_cranes_layout ON JibCranes(LayoutId);

-- ============================================================================
-- AGV STATIONS
-- ============================================================================
CREATE TABLE IF NOT EXISTS AGVStations (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    X REAL NOT NULL,
    Y REAL NOT NULL,
    Rotation REAL DEFAULT 0,
    StationType TEXT DEFAULT 'pickup',
    ServiceTime REAL DEFAULT 30,
    DwellTime REAL DEFAULT 5,
    QueueCapacity INTEGER DEFAULT 3,
    IsBlocking INTEGER DEFAULT 0,
    IsHoming INTEGER DEFAULT 0,
    Priority INTEGER DEFAULT 0,
    Color TEXT DEFAULT '#9B59B6',
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_agv_stations_layout ON AGVStations(LayoutId);

-- ============================================================================
-- CONVEYORS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Conveyors (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    ConveyorType TEXT DEFAULT 'belt',
    Direction TEXT DEFAULT 'forward',
    IsAccumulating INTEGER DEFAULT 0,
    Width REAL DEFAULT 24,
    Speed REAL DEFAULT 60,
    Capacity INTEGER DEFAULT 10,
    Color TEXT DEFAULT '#95A5A6',
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_conveyors_layout ON Conveyors(LayoutId);

-- ============================================================================
-- OPENINGS
-- ============================================================================
CREATE TABLE IF NOT EXISTS Openings (
    Id TEXT PRIMARY KEY,
    LayoutId TEXT NOT NULL REFERENCES Layouts(Id) ON DELETE CASCADE,
    Name TEXT NOT NULL,
    X REAL NOT NULL,
    Y REAL NOT NULL,
    Rotation REAL DEFAULT 0,
    OpeningType TEXT DEFAULT 'door',
    State TEXT DEFAULT 'open',
    ClearWidth REAL DEFAULT 36,
    ClearHeight REAL DEFAULT 80,
    Capacity INTEGER DEFAULT 1,
    TraversalTime REAL DEFAULT 2.0,
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_openings_layout ON Openings(LayoutId);
