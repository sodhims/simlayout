using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Serialization;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Mapper for EOTCraneData domain model
    /// </summary>
    public class EOTCraneMapper : IElementMapper<EOTCraneData>
    {
        public string ElementType => DbElementType.EOTCrane;

        public ElementDto ToDto(EOTCraneData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.LocalFlow,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public EOTCraneData FromDto(ElementDto dto)
        {
            var crane = JsonSerializationHelper.Deserialize<EOTCraneData>(dto.PropertiesJson);

            if (crane == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize EOTCraneData from DTO {dto.Id}");
            }

            return crane;
        }
    }
}
