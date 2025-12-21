using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// SQLite implementation of IElementRepository
    /// </summary>
    public class SQLiteElementRepository : IElementRepository
    {
        private readonly DatabaseManager _dbManager;

        public SQLiteElementRepository(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<ElementDto?> GetByIdAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate
                FROM Elements
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<ElementDto>> GetByLayoutIdAsync(string layoutId)
        {
            var elements = new List<ElementDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate
                FROM Elements
                WHERE LayoutId = @layoutId
                ORDER BY Layer, ElementType, Name";

            command.Parameters.AddWithValue("@layoutId", layoutId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                elements.Add(MapFromReader(reader));
            }

            return elements;
        }

        public async Task<IEnumerable<ElementDto>> GetByLayoutAndTypeAsync(string layoutId, string elementType)
        {
            var elements = new List<ElementDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate
                FROM Elements
                WHERE LayoutId = @layoutId AND ElementType = @elementType
                ORDER BY Name";

            command.Parameters.AddWithValue("@layoutId", layoutId);
            command.Parameters.AddWithValue("@elementType", elementType);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                elements.Add(MapFromReader(reader));
            }

            return elements;
        }

        public async Task<IEnumerable<ElementDto>> GetByLayoutAndLayerAsync(string layoutId, int layer)
        {
            var elements = new List<ElementDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate
                FROM Elements
                WHERE LayoutId = @layoutId AND Layer = @layer
                ORDER BY ElementType, Name";

            command.Parameters.AddWithValue("@layoutId", layoutId);
            command.Parameters.AddWithValue("@layer", layer);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                elements.Add(MapFromReader(reader));
            }

            return elements;
        }

        public async Task<bool> InsertAsync(ElementDto element)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Elements (Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate)
                VALUES (@id, @layoutId, @elementType, @layer, @name, @propertiesJson, @createdDate, @modifiedDate)";

            command.Parameters.AddWithValue("@id", element.Id);
            command.Parameters.AddWithValue("@layoutId", element.LayoutId);
            command.Parameters.AddWithValue("@elementType", element.ElementType);
            command.Parameters.AddWithValue("@layer", element.Layer);
            command.Parameters.AddWithValue("@name", element.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@propertiesJson", element.PropertiesJson);
            command.Parameters.AddWithValue("@createdDate", element.CreatedDate.ToString("o"));
            command.Parameters.AddWithValue("@modifiedDate", element.ModifiedDate.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(ElementDto element)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Elements
                SET LayoutId = @layoutId,
                    ElementType = @elementType,
                    Layer = @layer,
                    Name = @name,
                    PropertiesJson = @propertiesJson,
                    ModifiedDate = @modifiedDate
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", element.Id);
            command.Parameters.AddWithValue("@layoutId", element.LayoutId);
            command.Parameters.AddWithValue("@elementType", element.ElementType);
            command.Parameters.AddWithValue("@layer", element.Layer);
            command.Parameters.AddWithValue("@name", element.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@propertiesJson", element.PropertiesJson);
            command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Elements WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> DeleteByLayoutIdAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Elements WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Elements WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> GetCountAsync(string layoutId)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Elements WHERE LayoutId = @layoutId";
            command.Parameters.AddWithValue("@layoutId", layoutId);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> BatchInsertAsync(IEnumerable<ElementDto> elements)
        {
            using var connection = _dbManager.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int count = 0;
                foreach (var element in elements)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = @"
                        INSERT INTO Elements (Id, LayoutId, ElementType, Layer, Name, PropertiesJson, CreatedDate, ModifiedDate)
                        VALUES (@id, @layoutId, @elementType, @layer, @name, @propertiesJson, @createdDate, @modifiedDate)";

                    command.Parameters.AddWithValue("@id", element.Id);
                    command.Parameters.AddWithValue("@layoutId", element.LayoutId);
                    command.Parameters.AddWithValue("@elementType", element.ElementType);
                    command.Parameters.AddWithValue("@layer", element.Layer);
                    command.Parameters.AddWithValue("@name", element.Name ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@propertiesJson", element.PropertiesJson);
                    command.Parameters.AddWithValue("@createdDate", element.CreatedDate.ToString("o"));
                    command.Parameters.AddWithValue("@modifiedDate", element.ModifiedDate.ToString("o"));

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

        public async Task<int> BatchUpdateAsync(IEnumerable<ElementDto> elements)
        {
            using var connection = _dbManager.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int count = 0;
                foreach (var element in elements)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Elements
                        SET LayoutId = @layoutId,
                            ElementType = @elementType,
                            Layer = @layer,
                            Name = @name,
                            PropertiesJson = @propertiesJson,
                            ModifiedDate = @modifiedDate
                        WHERE Id = @id";

                    command.Parameters.AddWithValue("@id", element.Id);
                    command.Parameters.AddWithValue("@layoutId", element.LayoutId);
                    command.Parameters.AddWithValue("@elementType", element.ElementType);
                    command.Parameters.AddWithValue("@layer", element.Layer);
                    command.Parameters.AddWithValue("@name", element.Name ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@propertiesJson", element.PropertiesJson);
                    command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));

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
        /// Maps a SqliteDataReader row to an ElementDto
        /// </summary>
        private ElementDto MapFromReader(SqliteDataReader reader)
        {
            return new ElementDto
            {
                Id = reader.GetString(0),
                LayoutId = reader.GetString(1),
                ElementType = reader.GetString(2),
                Layer = reader.GetInt32(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                PropertiesJson = reader.GetString(5),
                CreatedDate = DateTime.Parse(reader.GetString(6)),
                ModifiedDate = DateTime.Parse(reader.GetString(7))
            };
        }
    }
}
