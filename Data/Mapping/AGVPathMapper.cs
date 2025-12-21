using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Serialization;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Mapper for AGVPathData domain model
    /// </summary>
    public class AGVPathMapper : IElementMapper<AGVPathData>
    {
        public string ElementType => DbElementType.AGVPath;

        public ElementDto ToDto(AGVPathData domain, string layoutId)
        {
            var propertiesJson = JsonSerializationHelper.Serialize(domain);

            return new ElementDto
            {
                Id = domain.Id,
                LayoutId = layoutId,
                ElementType = ElementType,
                Layer = (int)LayerType.GuidedTransport,
                Name = domain.Name,
                PropertiesJson = propertiesJson,
                CreatedDate = System.DateTime.UtcNow,
                ModifiedDate = System.DateTime.UtcNow
            };
        }

        public AGVPathData FromDto(ElementDto dto)
        {
            var path = JsonSerializationHelper.Deserialize<AGVPathData>(dto.PropertiesJson);

            if (path == null)
            {
                throw new System.InvalidOperationException($"Failed to deserialize AGVPathData from DTO {dto.Id}");
            }

            return path;
        }
    }
}
