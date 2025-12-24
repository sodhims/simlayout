using System.Data;
using Dapper;
using FactorySimulation.Core.Models;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository implementation for Part Variant operations
/// </summary>
public class PartVariantRepository : IPartVariantRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public PartVariantRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public PartVariantRepository() : this(() => DatabaseConfiguration.CreateConnection())
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

    public async Task<IReadOnlyList<PartVariant>> GetAllAsync()
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                ORDER BY v.PartNumber
                """;

            var variants = await connection.QueryAsync<PartVariant>(sql);
            return variants.ToList().AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<PartVariant?> GetByIdAsync(int id)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE v.Id = @Id
                """;

            return await connection.QueryFirstOrDefaultAsync<PartVariant>(sql, new { Id = id });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<PartVariant?> GetByPartNumberAsync(string partNumber)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE v.PartNumber = @PartNumber
                """;

            return await connection.QueryFirstOrDefaultAsync<PartVariant>(sql, new { PartNumber = partNumber });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<IReadOnlyList<PartVariant>> GetByFamilyAsync(int familyId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT v.Id, v.FamilyId, v.PartNumber, v.Name, v.Description, v.IsActive, v.CreatedAt, v.ModifiedAt,
                       f.FamilyCode, f.Name as FamilyName, f.CategoryId, c.Name as CategoryName
                FROM part_Variants v
                JOIN part_Families f ON v.FamilyId = f.Id
                LEFT JOIN part_Categories c ON f.CategoryId = c.Id
                WHERE v.FamilyId = @FamilyId
                ORDER BY v.PartNumber
                """;

            var variants = await connection.QueryAsync<PartVariant>(sql, new { FamilyId = familyId });
            return variants.ToList().AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<int> CreateAsync(PartVariant variant)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                INSERT INTO part_Variants (FamilyId, PartNumber, Name, Description, IsActive, CreatedAt, ModifiedAt)
                VALUES (@FamilyId, @PartNumber, @Name, @Description, @IsActive, datetime('now'), datetime('now'));
                SELECT last_insert_rowid();
                """;

            var id = await connection.ExecuteScalarAsync<int>(sql, variant);
            return id;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Duplicate PartNumber: '{variant.PartNumber}' already exists", ex);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task UpdateAsync(PartVariant variant)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                UPDATE part_Variants
                SET FamilyId = @FamilyId,
                    PartNumber = @PartNumber,
                    Name = @Name,
                    Description = @Description,
                    IsActive = @IsActive,
                    ModifiedAt = datetime('now')
                WHERE Id = @Id
                """;

            await connection.ExecuteAsync(sql, variant);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Duplicate PartNumber: '{variant.PartNumber}' already exists", ex);
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
            const string sql = "DELETE FROM part_Variants WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
