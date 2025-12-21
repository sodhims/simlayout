using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// SQLite implementation of ILayoutRepository
    /// </summary>
    public class SQLiteLayoutRepository : ILayoutRepository
    {
        private readonly DatabaseManager _dbManager;

        public SQLiteLayoutRepository(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<LayoutDto?> GetByIdAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Name, Width, Height, Unit, CreatedDate, ModifiedDate, Version
                FROM Layouts
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<LayoutDto>> GetAllAsync()
        {
            var layouts = new List<LayoutDto>();

            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Name, Width, Height, Unit, CreatedDate, ModifiedDate, Version
                FROM Layouts
                ORDER BY ModifiedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                layouts.Add(MapFromReader(reader));
            }

            return layouts;
        }

        public async Task<bool> InsertAsync(LayoutDto layout)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Layouts (Id, Name, Width, Height, Unit, CreatedDate, ModifiedDate, Version)
                VALUES (@id, @name, @width, @height, @unit, @createdDate, @modifiedDate, @version)";

            command.Parameters.AddWithValue("@id", layout.Id);
            command.Parameters.AddWithValue("@name", layout.Name);
            command.Parameters.AddWithValue("@width", layout.Width);
            command.Parameters.AddWithValue("@height", layout.Height);
            command.Parameters.AddWithValue("@unit", layout.Unit);
            command.Parameters.AddWithValue("@createdDate", layout.CreatedDate.ToString("o"));
            command.Parameters.AddWithValue("@modifiedDate", layout.ModifiedDate.ToString("o"));
            command.Parameters.AddWithValue("@version", layout.Version);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(LayoutDto layout)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Layouts
                SET Name = @name,
                    Width = @width,
                    Height = @height,
                    Unit = @unit,
                    ModifiedDate = @modifiedDate,
                    Version = @version
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", layout.Id);
            command.Parameters.AddWithValue("@name", layout.Name);
            command.Parameters.AddWithValue("@width", layout.Width);
            command.Parameters.AddWithValue("@height", layout.Height);
            command.Parameters.AddWithValue("@unit", layout.Unit);
            command.Parameters.AddWithValue("@modifiedDate", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@version", layout.Version);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Layouts WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Layouts WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> GetCountAsync()
        {
            using var connection = _dbManager.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Layouts";

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        /// <summary>
        /// Maps a SqliteDataReader row to a LayoutDto
        /// </summary>
        private LayoutDto MapFromReader(SqliteDataReader reader)
        {
            return new LayoutDto
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Width = reader.GetDouble(2),
                Height = reader.GetDouble(3),
                Unit = reader.GetString(4),
                CreatedDate = DateTime.Parse(reader.GetString(5)),
                ModifiedDate = DateTime.Parse(reader.GetString(6)),
                Version = reader.GetInt32(7)
            };
        }
    }
}
