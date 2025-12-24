using System.Data;
using Dapper;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// Repository implementation for Variant Properties operations
/// </summary>
public class VariantPropertiesRepository : IVariantPropertiesRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public VariantPropertiesRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public VariantPropertiesRepository() : this(() => DatabaseConfiguration.CreateConnection())
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

    public async Task<VariantProperties?> GetByVariantIdAsync(int variantId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT Id, VariantId, LengthMm, WidthMm, HeightMm, WeightKg,
                       ContainerType, UnitsPerContainer, RequiresForklift, Notes
                FROM part_VariantProperties
                WHERE VariantId = @VariantId
                """;

            return await connection.QueryFirstOrDefaultAsync<VariantProperties>(sql, new { VariantId = variantId });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task SaveAsync(VariantProperties properties)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            // Check if record exists
            var existing = await GetByVariantIdAsync(properties.VariantId);

            if (existing == null)
            {
                // Insert
                const string insertSql = """
                    INSERT INTO part_VariantProperties
                        (VariantId, LengthMm, WidthMm, HeightMm, WeightKg, ContainerType, UnitsPerContainer, RequiresForklift, Notes)
                    VALUES
                        (@VariantId, @LengthMm, @WidthMm, @HeightMm, @WeightKg, @ContainerType, @UnitsPerContainer, @RequiresForklift, @Notes);
                    SELECT last_insert_rowid();
                    """;

                properties.Id = await connection.ExecuteScalarAsync<int>(insertSql, properties);
            }
            else
            {
                // Update
                properties.Id = existing.Id;
                const string updateSql = """
                    UPDATE part_VariantProperties
                    SET LengthMm = @LengthMm,
                        WidthMm = @WidthMm,
                        HeightMm = @HeightMm,
                        WeightKg = @WeightKg,
                        ContainerType = @ContainerType,
                        UnitsPerContainer = @UnitsPerContainer,
                        RequiresForklift = @RequiresForklift,
                        Notes = @Notes
                    WHERE VariantId = @VariantId
                    """;

                await connection.ExecuteAsync(updateSql, properties);
            }
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task<FamilyDefaults?> GetFamilyDefaultsAsync(int familyId)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            const string sql = """
                SELECT Id, FamilyId, LengthMm, WidthMm, HeightMm, WeightKg,
                       ContainerType, UnitsPerContainer, RequiresForklift, Notes
                FROM part_FamilyDefaults
                WHERE FamilyId = @FamilyId
                """;

            return await connection.QueryFirstOrDefaultAsync<FamilyDefaults>(sql, new { FamilyId = familyId });
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    public async Task SaveFamilyDefaultsAsync(FamilyDefaults defaults)
    {
        var (connection, shouldDispose) = GetConnection();
        try
        {
            // Check if record exists
            var existing = await GetFamilyDefaultsAsync(defaults.FamilyId);

            if (existing == null)
            {
                // Insert
                const string insertSql = """
                    INSERT INTO part_FamilyDefaults
                        (FamilyId, LengthMm, WidthMm, HeightMm, WeightKg, ContainerType, UnitsPerContainer, RequiresForklift, Notes)
                    VALUES
                        (@FamilyId, @LengthMm, @WidthMm, @HeightMm, @WeightKg, @ContainerType, @UnitsPerContainer, @RequiresForklift, @Notes);
                    SELECT last_insert_rowid();
                    """;

                defaults.Id = await connection.ExecuteScalarAsync<int>(insertSql, defaults);
            }
            else
            {
                // Update
                defaults.Id = existing.Id;
                const string updateSql = """
                    UPDATE part_FamilyDefaults
                    SET LengthMm = @LengthMm,
                        WidthMm = @WidthMm,
                        HeightMm = @HeightMm,
                        WeightKg = @WeightKg,
                        ContainerType = @ContainerType,
                        UnitsPerContainer = @UnitsPerContainer,
                        RequiresForklift = @RequiresForklift,
                        Notes = @Notes
                    WHERE FamilyId = @FamilyId
                    """;

                await connection.ExecuteAsync(updateSql, defaults);
            }
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
