using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Serialization;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Mapper for ConveyorData domain model
    /// </summary>
    public class ConveyorMapper : IElementMapper<ConveyorData>
    {
        public string ElementType => DbElementType.Conveyor;

        public ElementDto ToDto(ConveyorData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.Equipment,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public ConveyorData FromDto(ElementDto dto)
        {
            var conveyor = JsonSerializationHelper.Deserialize<ConveyorData>(dto.PropertiesJson);

            if (conveyor == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize ConveyorData from DTO {dto.Id}");
            }

            return conveyor;
        }
    }
}
