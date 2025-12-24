using Dapper;
using FactorySimulation.Core.Models;

namespace FactorySimulation.Data.Repositories;

/// <summary>
/// SQLite implementation of the BOM repository using Dapper
/// </summary>
public class BomRepository : IBomRepository
{
    private readonly IPartTypeRepository _partTypeRepository;

    public BomRepository(IPartTypeRepository partTypeRepository)
    {
        _partTypeRepository = partTypeRepository;
    }

    public async Task<BillOfMaterials?> GetByIdAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, PartTypeId, Version, IsActive, EffectiveDate, ExpirationDate,
                   CreatedAt, ModifiedAt
            FROM BillOfMaterials
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<BomDto>(sql, new { Id = id });
        if (dto == null) return null;

        var bom = MapToBom(dto);
        bom.ParentPart = await _partTypeRepository.GetByIdAsync(bom.PartTypeId);
        bom.Items = (await GetItemsByBomIdAsync(bom.Id)).ToList();

        return bom;
    }

    public async Task<BillOfMaterials?> GetByPartTypeIdAsync(int partTypeId, bool activeOnly = true)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        var sql = """
            SELECT Id, PartTypeId, Version, IsActive, EffectiveDate, ExpirationDate,
                   CreatedAt, ModifiedAt
            FROM BillOfMaterials
            WHERE PartTypeId = @PartTypeId
            """;

        if (activeOnly)
        {
            sql += " AND IsActive = 1";
        }

        sql += " ORDER BY Version DESC LIMIT 1";

        var dto = await connection.QuerySingleOrDefaultAsync<BomDto>(sql, new { PartTypeId = partTypeId });
        if (dto == null) return null;

        var bom = MapToBom(dto);
        bom.ParentPart = await _partTypeRepository.GetByIdAsync(bom.PartTypeId);
        bom.Items = (await GetItemsByBomIdAsync(bom.Id)).ToList();

        return bom;
    }

    public async Task<int> CreateAsync(BillOfMaterials bom)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO BillOfMaterials (PartTypeId, Version, IsActive, EffectiveDate,
                                         ExpirationDate, CreatedAt, ModifiedAt)
            VALUES (@PartTypeId, @Version, @IsActive, @EffectiveDate,
                    @ExpirationDate, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
            """;

        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            bom.PartTypeId,
            bom.Version,
            IsActive = bom.IsActive ? 1 : 0,
            EffectiveDate = bom.EffectiveDate.ToString("yyyy-MM-dd HH:mm:ss"),
            ExpirationDate = bom.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss")
        });

        bom.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(BillOfMaterials bom)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE BillOfMaterials
            SET Version = @Version,
                IsActive = @IsActive,
                EffectiveDate = @EffectiveDate,
                ExpirationDate = @ExpirationDate,
                ModifiedAt = datetime('now')
            WHERE Id = @Id
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            bom.Id,
            bom.Version,
            IsActive = bom.IsActive ? 1 : 0,
            EffectiveDate = bom.EffectiveDate.ToString("yyyy-MM-dd HH:mm:ss"),
            ExpirationDate = bom.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss")
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        // First delete all items
        await ClearItemsAsync(id);

        const string sql = "DELETE FROM BillOfMaterials WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<BOMItem>> GetItemsByBomIdAsync(int bomId)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT bi.Id, bi.BomId, bi.ComponentPartTypeId, bi.Quantity,
                   bi.UnitOfMeasure, bi.Sequence, bi.Notes, bi.CreatedAt,
                   pt.Id as PartId, pt.PartNumber, pt.Name as PartName,
                   pt.Category, pt.UnitOfMeasure as PartUnit
            FROM BOMItems bi
            JOIN PartTypes pt ON bi.ComponentPartTypeId = pt.Id
            WHERE bi.BomId = @BomId
            ORDER BY bi.Sequence, pt.PartNumber
            """;

        var results = await connection.QueryAsync<BomItemDto>(sql, new { BomId = bomId });
        return results.Select(MapToBomItem);
    }

    public async Task<int> AddItemAsync(BOMItem item)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        // Get next sequence number
        var maxSeq = await connection.ExecuteScalarAsync<int?>(
            "SELECT MAX(Sequence) FROM BOMItems WHERE BomId = @BomId",
            new { item.BomId });

        item.Sequence = (maxSeq ?? 0) + 10;

        const string sql = """
            INSERT INTO BOMItems (BomId, ComponentPartTypeId, Quantity, UnitOfMeasure,
                                  Sequence, Notes, CreatedAt)
            VALUES (@BomId, @ComponentPartTypeId, @Quantity, @UnitOfMeasure,
                    @Sequence, @Notes, datetime('now'));
            SELECT last_insert_rowid();
            """;

        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            item.BomId,
            item.ComponentPartTypeId,
            item.Quantity,
            item.UnitOfMeasure,
            item.Sequence,
            item.Notes
        });

        item.Id = id;
        return id;
    }

    public async Task<bool> UpdateItemAsync(BOMItem item)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE BOMItems
            SET Quantity = @Quantity,
                UnitOfMeasure = @UnitOfMeasure,
                Sequence = @Sequence,
                Notes = @Notes
            WHERE Id = @Id
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            item.Id,
            item.Quantity,
            item.UnitOfMeasure,
            item.Sequence,
            item.Notes
        });

        return rowsAffected > 0;
    }

    public async Task<bool> RemoveItemAsync(int itemId)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM BOMItems WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = itemId });
        return rowsAffected > 0;
    }

    public async Task<bool> ClearItemsAsync(int bomId)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM BOMItems WHERE BomId = @BomId";
        await connection.ExecuteAsync(sql, new { BomId = bomId });
        return true;
    }

    public async Task<IEnumerable<PartType>> GetWhereUsedAsync(int partTypeId)
    {
        await using var connection = DatabaseConfiguration.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT DISTINCT pt.Id, pt.PartNumber, pt.Name, pt.Description,
                   pt.Category, pt.UnitOfMeasure, pt.UnitCost, pt.ScenarioId,
                   pt.CreatedAt, pt.ModifiedAt
            FROM PartTypes pt
            JOIN BillOfMaterials bom ON pt.Id = bom.PartTypeId
            JOIN BOMItems bi ON bom.Id = bi.BomId
            WHERE bi.ComponentPartTypeId = @PartTypeId AND bom.IsActive = 1
            ORDER BY pt.PartNumber
            """;

        var results = await connection.QueryAsync<PartTypeDto>(sql, new { PartTypeId = partTypeId });
        return results.Select(MapToPartType);
    }

    public async Task<IEnumerable<int>> GetAncestorIdsAsync(int partTypeId)
    {
        var ancestors = new HashSet<int>();
        await GetAncestorsRecursive(partTypeId, ancestors);
        return ancestors;
    }

    private async Task GetAncestorsRecursive(int partTypeId, HashSet<int> ancestors)
    {
        var parents = await GetWhereUsedAsync(partTypeId);
        foreach (var parent in parents)
        {
            if (ancestors.Add(parent.Id))
            {
                await GetAncestorsRecursive(parent.Id, ancestors);
            }
        }
    }

    public async Task<bool> WouldCreateCycleAsync(int parentPartTypeId, int childPartTypeId)
    {
        // Adding child to parent would create cycle if:
        // 1. Child is same as parent
        // 2. Parent is already an ancestor of child (child contains parent in its tree)

        if (parentPartTypeId == childPartTypeId)
            return true;

        // Get all ancestors of the parent - if child is among them, it's a cycle
        var parentAncestors = await GetAncestorIdsAsync(parentPartTypeId);
        if (parentAncestors.Contains(childPartTypeId))
            return true;

        // Also check if parent appears anywhere in child's BOM tree
        var childDescendants = new HashSet<int>();
        await GetDescendantsRecursive(childPartTypeId, childDescendants);

        return childDescendants.Contains(parentPartTypeId);
    }

    private async Task GetDescendantsRecursive(int partTypeId, HashSet<int> descendants)
    {
        var bom = await GetByPartTypeIdAsync(partTypeId);
        if (bom?.Items == null) return;

        foreach (var item in bom.Items)
        {
            if (descendants.Add(item.ComponentPartTypeId))
            {
                await GetDescendantsRecursive(item.ComponentPartTypeId, descendants);
            }
        }
    }

    // DTOs for database mapping
    private class BomDto
    {
        public int Id { get; set; }
        public int PartTypeId { get; set; }
        public int Version { get; set; }
        public int IsActive { get; set; }
        public string? EffectiveDate { get; set; }
        public string? ExpirationDate { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
    }

    private class BomItemDto
    {
        public int Id { get; set; }
        public int BomId { get; set; }
        public int ComponentPartTypeId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = "EA";
        public int Sequence { get; set; }
        public string? Notes { get; set; }
        public string? CreatedAt { get; set; }
        // Joined part data
        public int PartId { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public int Category { get; set; }
        public string PartUnit { get; set; } = "EA";
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

    private static BillOfMaterials MapToBom(BomDto dto)
    {
        return new BillOfMaterials
        {
            Id = dto.Id,
            PartTypeId = dto.PartTypeId,
            Version = dto.Version,
            IsActive = dto.IsActive == 1,
            EffectiveDate = ParseDateTime(dto.EffectiveDate),
            ExpirationDate = string.IsNullOrEmpty(dto.ExpirationDate) ? null : ParseDateTime(dto.ExpirationDate),
            CreatedAt = ParseDateTime(dto.CreatedAt),
            ModifiedAt = ParseDateTime(dto.ModifiedAt)
        };
    }

    private static BOMItem MapToBomItem(BomItemDto dto)
    {
        return new BOMItem
        {
            Id = dto.Id,
            BomId = dto.BomId,
            ComponentPartTypeId = dto.ComponentPartTypeId,
            Quantity = dto.Quantity,
            UnitOfMeasure = dto.UnitOfMeasure,
            Sequence = dto.Sequence,
            Notes = dto.Notes,
            CreatedAt = ParseDateTime(dto.CreatedAt),
            ComponentPart = new PartType
            {
                Id = dto.PartId,
                PartNumber = dto.PartNumber,
                Name = dto.PartName,
                Category = (PartCategory)dto.Category,
                UnitOfMeasure = dto.PartUnit
            }
        };
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
