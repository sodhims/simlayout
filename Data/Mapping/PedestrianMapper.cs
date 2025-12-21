using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Serialization;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Mapper for WalkwayData domain model
    /// </summary>
    public class WalkwayMapper : IElementMapper<WalkwayData>
    {
        public string ElementType => DbElementType.Walkway;

        public ElementDto ToDto(WalkwayData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.Pedestrian,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public WalkwayData FromDto(ElementDto dto)
        {
            var walkway = JsonSerializationHelper.Deserialize<WalkwayData>(dto.PropertiesJson);

            if (walkway == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize WalkwayData from DTO {dto.Id}");
            }

            return walkway;
        }
    }

    /// <summary>
    /// Mapper for PedestrianCrossingData domain model
    /// </summary>
    public class PedestrianCrossingMapper : IElementMapper<PedestrianCrossingData>
    {
        public string ElementType => DbElementType.PedestrianCrossing;

        public ElementDto ToDto(PedestrianCrossingData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.Pedestrian,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public PedestrianCrossingData FromDto(ElementDto dto)
        {
            var crossing = JsonSerializationHelper.Deserialize<PedestrianCrossingData>(dto.PropertiesJson);

            if (crossing == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize PedestrianCrossingData from DTO {dto.Id}");
            }

            return crossing;
        }
    }

    /// <summary>
    /// Mapper for SafetyZoneData domain model
    /// </summary>
    public class SafetyZoneMapper : IElementMapper<SafetyZoneData>
    {
        public string ElementType => DbElementType.SafetyZone;

        public ElementDto ToDto(SafetyZoneData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.Pedestrian,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public SafetyZoneData FromDto(ElementDto dto)
        {
            var zone = JsonSerializationHelper.Deserialize<SafetyZoneData>(dto.PropertiesJson);

            if (zone == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize SafetyZoneData from DTO {dto.Id}");
            }

            return zone;
        }
    }
}
