using Dapper;
using FactorySimulation.Core.Models;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// SQLite implementation of the PartType repository using Dapper
/// </summary>
public class PartTypeRepository : IPartTypeRepository
{
    public async Task<IEnumerable<PartType>> GetAllAsync(int? scenarioId = null)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        string sql;
        if (scenarioId.HasValue)
        {
            sql = """
                SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                       UnitCost, ScenarioId, CreatedAt, ModifiedAt
                FROM PartTypes
                WHERE ScenarioId = @ScenarioId OR ScenarioId IS NULL
                ORDER BY PartNumber
                """;
            var results = await connection.QueryAsync<PartTypeDto>(sql, new { ScenarioId = scenarioId });
            return results.Select(MapToPartType);
        }
        else
        {
            sql = """
                SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                       UnitCost, ScenarioId, CreatedAt, ModifiedAt
                FROM PartTypes
                ORDER BY PartNumber
                """;
            var results = await connection.QueryAsync<PartTypeDto>(sql);
            return results.Select(MapToPartType);
        }
    }

    public async Task<PartType?> GetByIdAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                   UnitCost, ScenarioId, CreatedAt, ModifiedAt
            FROM PartTypes
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<PartTypeDto>(sql, new { Id = id });
        return dto != null ? MapToPartType(dto) : null;
    }

    public async Task<PartType?> GetByPartNumberAsync(string partNumber)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                   UnitCost, ScenarioId, CreatedAt, ModifiedAt
            FROM PartTypes
            WHERE PartNumber = @PartNumber
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<PartTypeDto>(sql, new { PartNumber = partNumber });
        return dto != null ? MapToPartType(dto) : null;
    }

    public async Task<int> CreateAsync(PartType partType)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO PartTypes (PartNumber, Name, Description, Category, UnitOfMeasure,
                                   UnitCost, ScenarioId, CreatedAt, ModifiedAt)
            VALUES (@PartNumber, @Name, @Description, @Category, @UnitOfMeasure,
                    @UnitCost, @ScenarioId, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
            """;

        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            partType.PartNumber,
            partType.Name,
            partType.Description,
            Category = (int)partType.Category,
            partType.UnitOfMeasure,
            partType.UnitCost,
            partType.ScenarioId
        });

        partType.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(PartType partType)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE PartTypes
            SET PartNumber = @PartNumber,
                Name = @Name,
                Description = @Description,
                Category = @Category,
                UnitOfMeasure = @UnitOfMeasure,
                UnitCost = @UnitCost,
                ModifiedAt = datetime('now')
            WHERE Id = @Id
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            partType.Id,
            partType.PartNumber,
            partType.Name,
            partType.Description,
            Category = (int)partType.Category,
            partType.UnitOfMeasure,
            partType.UnitCost
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM PartTypes WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<PartType>> GetByCategory(PartCategory category)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                   UnitCost, ScenarioId, CreatedAt, ModifiedAt
            FROM PartTypes
            WHERE Category = @Category
            ORDER BY PartNumber
            """;

        var results = await connection.QueryAsync<PartTypeDto>(sql, new { Category = (int)category });
        return results.Select(MapToPartType);
    }

    public async Task<IEnumerable<PartType>> SearchAsync(string searchTerm)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, PartNumber, Name, Description, Category, UnitOfMeasure,
                   UnitCost, ScenarioId, CreatedAt, ModifiedAt
            FROM PartTypes
            WHERE PartNumber LIKE @SearchTerm OR Name LIKE @SearchTerm
            ORDER BY PartNumber
            """;

        var results = await connection.QueryAsync<PartTypeDto>(sql, new { SearchTerm = $"%{searchTerm}%" });
        return results.Select(MapToPartType);
    }

    private class PartTypeDto
    {
        public int Id { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Category { get; set; }
        public string UnitOfMeasure { get; set; } = "EA";
        public decimal UnitCost { get; set; }
        public int? ScenarioId { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
    }

    private static PartType MapToPartType(PartTypeDto dto)
    {
        return new PartType
        {
            Id = dto.Id,
            PartNumber = dto.PartNumber,
            Name = dto.Name,
            Description = dto.Description,
            Category = (PartCategory)dto.Category,
            UnitOfMeasure = dto.UnitOfMeasure,
            UnitCost = dto.UnitCost,
            ScenarioId = dto.ScenarioId,
            CreatedAt = ParseDateTime(dto.CreatedAt),
            ModifiedAt = ParseDateTime(dto.ModifiedAt)
        };
    }

    private static DateTime ParseDateTime(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return DateTime.Now;
        return DateTime.TryParse(dateString, out var result) ? result : DateTime.Now;
    }
}
