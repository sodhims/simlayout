using System;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Serialization;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Mapper for ConflictResolutionData ↔ ElementDto
    /// </summary>
    public class ConflictResolutionMapper : IElementMapper<ConflictResolutionData>
    {
        public string ElementType => DbElementType.ConflictResolution;

        public ElementDto ToDto(ConflictResolutionData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = 8, // Conflict management layer
                Name = domain.ConflictType,
                PropertiesJson = propertiesJson,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
        }

        public ConflictResolutionData FromDto(ElementDto dto)
        {
            var domain = JsonSerializationHelper.Deserialize<ConflictResolutionData>(dto.PropertiesJson);
            if (domain != null)
            {
                domain.Id = dto.Id;
            }
            return domain ?? new ConflictResolutionData();
        }
    }

    /// <summary>
    /// Mapper for LayerConnectionData ↔ ElementDto
    /// </summary>
    public class LayerConnectionMapper : IElementMapper<LayerConnectionData>
    {
        public string ElementType => DbElementType.LayerConnection;

        public ElementDto ToDto(LayerConnectionData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = 8, // Conflict management layer
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
        }

        public LayerConnectionData FromDto(ElementDto dto)
        {
            var domain = JsonSerializationHelper.Deserialize<LayerConnectionData>(dto.PropertiesJson);
            if (domain != null)
            {
                domain.Id = dto.Id;
            }
            return domain ?? new LayerConnectionData();
        }
    }
}
