using System.Data;
using Dapper;
using FactorySimulation.Core.Models;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository implementation for Part Family operations
/// </summary>
public class PartFamilyRepository : IPartFamilyRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public PartFamilyRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public PartFamilyRepository() : this(() => DatabaseConfiguration.CreateConnection())
    {
    }

    private (IDbConnection Connection, bool ShouldDispose) GetConnection()
    {
        var connection = _connectionFactory();
        var wasAlreadyOpen = connection.State == ConnectionState.Open;
        if (!wasAlreadyOpen)
            connection.Open();
        return (connection, !wasAlreadyOpen);
    }

    public async Task<IReadOnlyList<PartFamily>> GetAllAsync()
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                ORDER BY f.Name
                """;

            var families = await connection.QueryAsync<PartFamily>(sql);
            return families.ToList().AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<PartFamily?> GetByIdAsync(int id)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE f.Id = @Id
                """;

            return await connection.QueryFirstOrDefaultAsync<PartFamily>(sql, new { Id = id });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<PartFamily?> GetByFamilyCodeAsync(string familyCode)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE f.FamilyCode = @FamilyCode
                """;

            return await connection.QueryFirstOrDefaultAsync<PartFamily>(sql, new { FamilyCode = familyCode });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<PartFamily?> GetWithVariantsAsync(int id)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string familySql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE f.Id = @Id
                """;

            var family = await connection.QueryFirstOrDefaultAsync<PartFamily>(familySql, new { Id = id });
            if (family == null) return null;

            const string variantsSql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE v.FamilyId = @FamilyId
                ORDER BY v.PartNumber
                """;

            var variants = await connection.QueryAsync<PartVariant>(variantsSql, new { FamilyId = id });
            family.Variants = variants.ToList();

            return family;
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<IReadOnlyList<PartFamily>> GetAllWithVariantsAsync()
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string familiesSql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                ORDER BY f.Name
                """;

            var families = (await connection.QueryAsync<PartFamily>(familiesSql)).ToList();
            if (families.Count == 0) return families.AsReadOnly();

            const string variantsSql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                ORDER BY v.PartNumber
                """;

            var variants = await connection.QueryAsync<PartVariant>(variantsSql);
            var variantsByFamily = variants.GroupBy(v => v.FamilyId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var family in families)
            {
                family.Variants = variantsByFamily.TryGetValue(family.Id, out var familyVariants)
                    ? familyVariants
                    : new List<PartVariant>();
            }

            return families.AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<IReadOnlyList<PartFamily>> SearchAsync(string searchTerm)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT f.Id, f.CategoryId, f.FamilyCode, f.Name, f.Description, f.IsActive, f.CreatedAt, f.ModifiedAt,
                       c.Name as CategoryName
                FROM part_Families f
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE LOWER(f.FamilyCode) LIKE @SearchTerm OR LOWER(f.Name) LIKE @SearchTerm
                ORDER BY f.Name
                """;

            var searchPattern = $"%{searchTerm.ToLower()}%";
            var families = await connection.QueryAsync<PartFamily>(sql, new { SearchTerm = searchPattern });
            return families.ToList().AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<int> CreateAsync(PartFamily family)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                INSERT INTO part_Families (CategoryId, FamilyCode, Name, Description, IsActive, CreatedAt, ModifiedAt)
                VALUES (@CategoryId, @FamilyCode, @Name, @Description, @IsActive, datetime('now'), datetime('now'));
                SELECT last_insert_rowid();
                """;

            var id = await connection.ExecuteScalarAsync<int>(sql, family);
            return id;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Duplicate FamilyCode: '{family.FamilyCode}' already exists", ex);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task UpdateAsync(PartFamily family)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                UPDATE part_Families
                SET CategoryId = @CategoryId,
                    FamilyCode = @FamilyCode,
                    Name = @Name,
                    Description = @Description,
                    IsActive = @IsActive,
                    ModifiedAt = datetime('now')
                WHERE Id = @Id
                """;

            await connection.ExecuteAsync(sql, family);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Duplicate FamilyCode: '{family.FamilyCode}' already exists", ex);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = "DELETE FROM part_Families WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
