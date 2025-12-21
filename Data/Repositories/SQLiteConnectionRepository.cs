using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// SQLite implementation of IConnectionRepository
    /// </summary>
    public class SQLiteConnectionRepository : IConnectionRepository
    {
        private readonly DatabaseManager _dbManager;

        public SQLiteConnectionRepository(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<ConnectionDto?> GetByIdAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<ConnectionDto>> GetByLayoutIdAsync(string layoutId)
        {
            var connections = new List<ConnectionDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE LayoutId = @layoutId
                ORDER BY ConnectionType, SourceElementId";

            command.Parameters.AddWithValue("@layoutId", layoutId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connections.Add(MapFromReader(reader));
            }

            return connections;
        }

        public async Task<IEnumerable<ConnectionDto>> GetByLayoutAndTypeAsync(string layoutId, string connectionType)
        {
            var connections = new List<ConnectionDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE LayoutId = @layoutId AND ConnectionType = @connectionType";

            command.Parameters.AddWithValue("@layoutId", layoutId);
            command.Parameters.AddWithValue("@connectionType", connectionType);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connections.Add(MapFromReader(reader));
            }

            return connections;
        }

        public async Task<IEnumerable<ConnectionDto>> GetBySourceElementAsync(string sourceElementId)
        {
            var connections = new List<ConnectionDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE SourceElementId = @sourceElementId";

            command.Parameters.AddWithValue("@sourceElementId", sourceElementId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connections.Add(MapFromReader(reader));
            }

            return connections;
        }

        public async Task<IEnumerable<ConnectionDto>> GetByTargetElementAsync(string targetElementId)
        {
            var connections = new List<ConnectionDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE TargetElementId = @targetElementId";

            command.Parameters.AddWithValue("@targetElementId", targetElementId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connections.Add(MapFromReader(reader));
            }

            return connections;
        }

        public async Task<IEnumerable<ConnectionDto>> GetBetweenElementsAsync(string sourceElementId, string targetElementId)
        {
            var connections = new List<ConnectionDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate
                FROM Connections
                WHERE SourceElementId = @sourceElementId AND TargetElementId = @targetElementId";

            command.Parameters.AddWithValue("@sourceElementId", sourceElementId);
            command.Parameters.AddWithValue("@targetElementId", targetElementId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                connections.Add(MapFromReader(reader));
            }

            return connections;
        }

        public async Task<bool> InsertAsync(ConnectionDto connection)
        {
            using var conn = _dbManager.GetConnection();
            using var command = conn.CreateCommand();

            command.CommandText = @"
                INSERT INTO Connections (Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate)
                VALUES (@id, @layoutId, @sourceElementId, @targetElementId, @connectionType, @propertiesJson, @createdDate)";

            command.Parameters.AddWithValue("@id", connection.Id);
            command.Parameters.AddWithValue("@layoutId", connection.LayoutId);
            command.Parameters.AddWithValue("@sourceElementId", connection.SourceElementId);
            command.Parameters.AddWithValue("@targetElementId", connection.TargetElementId);
            command.Parameters.AddWithValue("@connectionType", connection.ConnectionType);
            command.Parameters.AddWithValue("@propertiesJson", connection.PropertiesJson ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdDate", connection.CreatedDate.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(ConnectionDto connection)
        {
            using var conn = _dbManager.GetConnection();
            using var command = conn.CreateCommand();

            command.CommandText = @"
                UPDATE Connections
                SET LayoutId = @layoutId,
                    SourceElementId = @sourceElementId,
                    TargetElementId = @targetElementId,
                    ConnectionType = @connectionType,
                    PropertiesJson = @propertiesJson
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", connection.Id);
            command.Parameters.AddWithValue("@layoutId", connection.LayoutId);
            command.Parameters.AddWithValue("@sourceElementId", connection.SourceElementId);
            command.Parameters.AddWithValue("@targetElementId", connection.TargetElementId);
            command.Parameters.AddWithValue("@connectionType", connection.ConnectionType);
            command.Parameters.AddWithValue("@propertiesJson", connection.PropertiesJson ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Connections WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> DeleteByLayoutIdAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Connections WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Connections WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> GetCountAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Connections WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> BatchInsertAsync(IEnumerable<ConnectionDto> connections)
        {
            using var connection = _dbManager.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int count = 0;
                foreach (var conn in connections)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = @"
                        INSERT INTO Connections (Id, LayoutId, SourceElementId, TargetElementId, ConnectionType, PropertiesJson, CreatedDate)
                        VALUES (@id, @layoutId, @sourceElementId, @targetElementId, @connectionType, @propertiesJson, @createdDate)";

                    command.Parameters.AddWithValue("@id", conn.Id);
                    command.Parameters.AddWithValue("@layoutId", conn.LayoutId);
                    command.Parameters.AddWithValue("@sourceElementId", conn.SourceElementId);
                    command.Parameters.AddWithValue("@targetElementId", conn.TargetElementId);
                    command.Parameters.AddWithValue("@connectionType", conn.ConnectionType);
                    command.Parameters.AddWithValue("@propertiesJson", conn.PropertiesJson ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@createdDate", conn.CreatedDate.ToString("o"));

                    count += await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return count;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Maps a SqliteDataReader row to a ConnectionDto
        /// </summary>
        private ConnectionDto MapFromReader(SqliteDataReader reader)
        {
            return new ConnectionDto
            {
                Id = reader.GetString(0),
                LayoutId = reader.GetString(1),
                SourceElementId = reader.GetString(2),
                TargetElementId = reader.GetString(3),
                ConnectionType = reader.GetString(4),
                PropertiesJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedDate = DateTime.Parse(reader.GetString(6))
            };
        }
    }
}
