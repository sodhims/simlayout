using Dapper;
using FactorySimulation.Core.Models;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// SQLite implementation of the Scenario repository using Dapper
/// </summary>
public class ScenarioRepository : IScenarioRepository
{
    public async Task<IEnumerable<Scenario>> GetAllAsync()
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, Name, Description, ParentScenarioId, IsBase, CreatedAt, ModifiedAt
            FROM Scenarios
            ORDER BY IsBase DESC, ParentScenarioId NULLS FIRST, Name
            """;

        var scenarios = await connection.QueryAsync<ScenarioDto>(sql);
        return scenarios.Select(MapToScenario);
    }

    public async Task<Scenario?> GetByIdAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, Name, Description, ParentScenarioId, IsBase, CreatedAt, ModifiedAt
            FROM Scenarios
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<ScenarioDto>(sql, new { Id = id });
        return dto != null ? MapToScenario(dto) : null;
    }

    public async Task<int> CreateAsync(Scenario scenario)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO Scenarios (Name, Description, ParentScenarioId, IsBase, CreatedAt, ModifiedAt)
            VALUES (@Name, @Description, @ParentScenarioId, @IsBase, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
            """;

        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            scenario.Name,
            scenario.Description,
            ParentScenarioId = scenario.ParentScenarioId,
            IsBase = scenario.IsBase ? 1 : 0
        });

        scenario.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(Scenario scenario)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE Scenarios
            SET Name = @Name,
                Description = @Description,
                ParentScenarioId = @ParentScenarioId,
                ModifiedAt = datetime('now')
            WHERE Id = @Id AND IsBase = 0
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            scenario.Id,
            scenario.Name,
            scenario.Description,
            ParentScenarioId = scenario.ParentScenarioId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        // Don't allow deleting base scenario or scenarios with children
        const string sql = """
            DELETE FROM Scenarios
            WHERE Id = @Id
              AND IsBase = 0
              AND NOT EXISTS (SELECT 1 FROM Scenarios WHERE ParentScenarioId = @Id)
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<Scenario>> GetChildrenAsync(int parentId)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, Name, Description, ParentScenarioId, IsBase, CreatedAt, ModifiedAt
            FROM Scenarios
            WHERE ParentScenarioId = @ParentId
            ORDER BY Name
            """;

        var scenarios = await connection.QueryAsync<ScenarioDto>(sql, new { ParentId = parentId });
        return scenarios.Select(MapToScenario);
    }

    public async Task<bool> HasChildrenAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM Scenarios WHERE ParentScenarioId = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT COUNT(*) FROM Scenarios
            WHERE Name = @Name AND (@ExcludeId IS NULL OR Id != @ExcludeId)
            """;

        var count = await connection.ExecuteScalarAsync<int>(sql, new { Name = name, ExcludeId = excludeId });
        return count > 0;
    }

    // DTO for database mapping (handles SQLite date strings)
    private class ScenarioDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentScenarioId { get; set; }
        public int IsBase { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
    }

    private static Scenario MapToScenario(ScenarioDto dto)
    {
        return new Scenario
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            ParentScenarioId = dto.ParentScenarioId,
            IsBase = dto.IsBase == 1,
            CreatedAt = ParseDateTime(dto.CreatedAt),
            ModifiedAt = ParseDateTime(dto.ModifiedAt)
        };
    }

    private static DateTime ParseDateTime(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.Now;

        if (DateTime.TryParse(dateString, out var result))
            return result;

        return DateTime.Now;
    }
}
