using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// SQLite implementation of IZoneRepository
    /// </summary>
    public class SQLiteZoneRepository : IZoneRepository
    {
        private readonly DatabaseManager _dbManager;

        public SQLiteZoneRepository(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<ZoneDto?> GetByIdAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, Name, ZoneType, BoundaryJson, PropertiesJson, CreatedDate, ModifiedDate
                FROM Zones
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<ZoneDto>> GetByLayoutIdAsync(string layoutId)
        {
            var zones = new List<ZoneDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, Name, ZoneType, BoundaryJson, PropertiesJson, CreatedDate, ModifiedDate
                FROM Zones
                WHERE LayoutId = @layoutId
                ORDER BY ZoneType, Name";

            command.Parameters.AddWithValue("@layoutId", layoutId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                zones.Add(MapFromReader(reader));
            }

            return zones;
        }

        public async Task<IEnumerable<ZoneDto>> GetByLayoutAndTypeAsync(string layoutId, string zoneType)
        {
            var zones = new List<ZoneDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, Name, ZoneType, BoundaryJson, PropertiesJson, CreatedDate, ModifiedDate
                FROM Zones
                WHERE LayoutId = @layoutId AND ZoneType = @zoneType
                ORDER BY Name";

            command.Parameters.AddWithValue("@layoutId", layoutId);
            command.Parameters.AddWithValue("@zoneType", zoneType);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                zones.Add(MapFromReader(reader));
            }

            return zones;
        }

        public async Task<bool> InsertAsync(ZoneDto zone)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Zones (Id, LayoutId, Name, ZoneType, BoundaryJson, PropertiesJson, CreatedDate, ModifiedDate)
                VALUES (@id, @layoutId, @name, @zoneType, @boundaryJson, @propertiesJson, @createdDate, @modifiedDate)";

            command.Parameters.AddWithValue("@id", zone.Id);
            command.Parameters.AddWithValue("@layoutId", zone.LayoutId);
            command.Parameters.AddWithValue("@name", zone.Name);
            command.Parameters.AddWithValue("@zoneType", zone.ZoneType);
            command.Parameters.AddWithValue("@boundaryJson", zone.BoundaryJson ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@propertiesJson", zone.PropertiesJson ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdDate", zone.CreatedDate.ToString("o"));
            command.Parameters.AddWithValue("@modifiedDate", zone.ModifiedDate.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(ZoneDto zone)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Zones
                SET LayoutId = @layoutId,
                    Name = @name,
                    ZoneType = @zoneType,
                    BoundaryJson = @boundaryJson,
                    PropertiesJson = @propertiesJson,
                    ModifiedDate = @modifiedDate
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", zone.Id);
            command.Parameters.AddWithValue("@layoutId", zone.LayoutId);
            command.Parameters.AddWithValue("@name", zone.Name);
            command.Parameters.AddWithValue("@zoneType", zone.ZoneType);
            command.Parameters.AddWithValue("@boundaryJson", zone.BoundaryJson ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@propertiesJson", zone.PropertiesJson ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Zones WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> DeleteByLayoutIdAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Zones WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Zones WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> GetCountAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Zones WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> AddElementToZoneAsync(string zoneId, string elementId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT OR IGNORE INTO ElementZones (ElementId, ZoneId)
                VALUES (@elementId, @zoneId)";

            command.Parameters.AddWithValue("@elementId", elementId);
            command.Parameters.AddWithValue("@zoneId", zoneId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> RemoveElementFromZoneAsync(string zoneId, string elementId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                DELETE FROM ElementZones
                WHERE ElementId = @elementId AND ZoneId = @zoneId";

            command.Parameters.AddWithValue("@elementId", elementId);
            command.Parameters.AddWithValue("@zoneId", zoneId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<string>> GetElementsInZoneAsync(string zoneId)
        {
            var elementIds = new List<string>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT ElementId
                FROM ElementZones
                WHERE ZoneId = @zoneId";

            command.Parameters.AddWithValue("@zoneId", zoneId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                elementIds.Add(reader.GetString(0));
            }

            return elementIds;
        }

        public async Task<IEnumerable<string>> GetZonesForElementAsync(string elementId)
        {
            var zoneIds = new List<string>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT ZoneId
                FROM ElementZones
                WHERE ElementId = @elementId";

            command.Parameters.AddWithValue("@elementId", elementId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                zoneIds.Add(reader.GetString(0));
            }

            return zoneIds;
        }

        /// <summary>
        /// Maps a SqliteDataReader row to a ZoneDto
        /// </summary>
        private ZoneDto MapFromReader(SqliteDataReader reader)
        {
            return new ZoneDto
            {
                Id = reader.GetString(0),
                LayoutId = reader.GetString(1),
                Name = reader.GetString(2),
                ZoneType = reader.GetString(3),
                BoundaryJson = reader.IsDBNull(4) ? null : reader.GetString(4),
                PropertiesJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedDate = DateTime.Parse(reader.GetString(6)),
                ModifiedDate = DateTime.Parse(reader.GetString(7))
            };
        }
    }
}
