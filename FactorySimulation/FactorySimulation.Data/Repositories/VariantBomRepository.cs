using System.Data;
using Dapper;
using FactorySimulation.Core.Models;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository implementation for Variant Bill of Materials operations
/// </summary>
public class VariantBomRepository : IVariantBomRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public VariantBomRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public VariantBomRepository() : this(() => DatabaseConfiguration.CreateConnection())
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

    public async Task<VariantBillOfMaterials?> GetByVariantIdAsync(int variantId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT Id, VariantId, Version, IsActive, EffectiveDate, ExpirationDate, CreatedAt, ModifiedAt
                FROM variant_BillOfMaterials
                WHERE VariantId = @VariantId
                """;

            return await connection.QueryFirstOrDefaultAsync<VariantBillOfMaterials>(sql, new { VariantId = variantId });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<VariantBillOfMaterials?> GetWithItemsAsync(int variantId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string bomSql = """
                SELECT Id, VariantId, Version, IsActive, EffectiveDate, ExpirationDate, CreatedAt, ModifiedAt
                FROM variant_BillOfMaterials
                WHERE VariantId = @VariantId
                """;

            var bom = await connection.QueryFirstOrDefaultAsync<VariantBillOfMaterials>(bomSql, new { VariantId = variantId });
            if (bom == null) return null;

            const string itemsSql = """
                SELECT i.Id, i.BomId, i.ComponentVariantId, i.Quantity, i.UnitOfMeasure, i.Sequence, i.Notes, i.CreatedAt,
                       v.PartNumber, v.Name as VariantName, f.FamilyCode
                FROM variant_BOMItems i
                JOIN part_Variants v ON i.ComponentVariantId = v.Id
                JOIN part_Families f ON v.FamilyId = f.Id
                WHERE i.BomId = @BomId
                ORDER BY i.Sequence, i.Id
                """;

            var items = await connection.QueryAsync<VariantBOMItem, PartVariant, VariantBOMItem>(
                itemsSql,
                (item, variant) =>
                {
                    item.ComponentVariant = variant;
                    return item;
                },
                new { BomId = bom.Id },
                splitOn: "PartNumber");

            bom.Items = items.ToList();
            return bom;
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<int> SaveBomAsync(VariantBillOfMaterials bom)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            // Check if BOM exists
            var existing = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT Id FROM variant_BillOfMaterials WHERE VariantId = @VariantId",
                new { bom.VariantId });

            if (existing.HasValue)
            {
                // Update existing
                const string updateSql = """
                    UPDATE variant_BillOfMaterials
                    SET Version = @Version,
                        IsActive = @IsActive,
                        EffectiveDate = @EffectiveDate,
                        ExpirationDate = @ExpirationDate,
                        ModifiedAt = datetime('now')
                    WHERE VariantId = @VariantId
                    """;

                await connection.ExecuteAsync(updateSql, bom);
                return existing.Value;
            }
            else
            {
                // Insert new
                const string insertSql = """
                    INSERT INTO variant_BillOfMaterials (VariantId, Version, IsActive, EffectiveDate, ExpirationDate, CreatedAt, ModifiedAt)
                    VALUES (@VariantId, @Version, @IsActive, @EffectiveDate, @ExpirationDate, datetime('now'), datetime('now'));
                    SELECT last_insert_rowid();
                    """;

                return await connection.ExecuteScalarAsync<int>(insertSql, bom);
            }
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task DeleteBomAsync(int variantId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            // Items will be deleted by CASCADE
            const string sql = "DELETE FROM variant_BillOfMaterials WHERE VariantId = @VariantId";
            await connection.ExecuteAsync(sql, new { VariantId = variantId });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<int> AddItemAsync(VariantBOMItem item)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                INSERT INTO variant_BOMItems (BomId, ComponentVariantId, Quantity, UnitOfMeasure, Sequence, Notes, CreatedAt)
                VALUES (@BomId, @ComponentVariantId, @Quantity, @UnitOfMeasure, @Sequence, @Notes, datetime('now'));
                SELECT last_insert_rowid();
                """;

            return await connection.ExecuteScalarAsync<int>(sql, item);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Cannot add item: component variant does not exist or would violate constraints", ex);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task UpdateItemAsync(VariantBOMItem item)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                UPDATE variant_BOMItems
                SET ComponentVariantId = @ComponentVariantId,
                    Quantity = @Quantity,
                    UnitOfMeasure = @UnitOfMeasure,
                    Sequence = @Sequence,
                    Notes = @Notes
                WHERE Id = @Id
                """;

            await connection.ExecuteAsync(sql, item);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task DeleteItemAsync(int itemId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = "DELETE FROM variant_BOMItems WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = itemId });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<IReadOnlyList<VariantBOMItem>> GetItemsAsync(int bomId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT i.Id, i.BomId, i.ComponentVariantId, i.Quantity, i.UnitOfMeasure, i.Sequence, i.Notes, i.CreatedAt,
                       v.PartNumber, v.Name as VariantName, f.FamilyCode
                FROM variant_BOMItems i
                JOIN part_Variants v ON i.ComponentVariantId = v.Id
                JOIN part_Families f ON v.FamilyId = f.Id
                WHERE i.BomId = @BomId
                ORDER BY i.Sequence, i.Id
                """;

            var items = await connection.QueryAsync<VariantBOMItem, PartVariant, VariantBOMItem>(
                sql,
                (item, variant) =>
                {
                    item.ComponentVariant = variant;
                    return item;
                },
                new { BomId = bomId },
                splitOn: "PartNumber");

            return items.ToList().AsReadOnly();
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<bool> WouldCreateCircularReferenceAsync(int parentVariantId, int componentVariantId)
    {
        // If adding componentVariantId as a child of parentVariantId,
        // we need to check if parentVariantId appears anywhere in componentVariantId's BOM tree

        if (parentVariantId == componentVariantId)
            return true;

        var (connection, shouldDispose) = GetConnection();
        try
        {
            // Recursive CTE to find all descendants of the component
            const string sql = """
                WITH RECURSIVE ComponentTree AS (
                    -- Base: direct children of the component
                    SELECT i.ComponentVariantId
                    FROM variant_BillOfMaterials b
                    JOIN variant_BOMItems i ON b.Id = i.BomId
                    WHERE b.VariantId = @ComponentVariantId

                    UNION ALL

                    -- Recursive: children of children
                    SELECT i.ComponentVariantId
                    FROM ComponentTree ct
                    JOIN variant_BillOfMaterials b ON b.VariantId = ct.ComponentVariantId
                    JOIN variant_BOMItems i ON b.Id = i.BomId
                )
                SELECT COUNT(*) FROM ComponentTree WHERE ComponentVariantId = @ParentVariantId
                """;

            var count = await connection.ExecuteScalarAsync<int>(sql, new { ParentVariantId = parentVariantId, ComponentVariantId = componentVariantId });
            return count > 0;
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
