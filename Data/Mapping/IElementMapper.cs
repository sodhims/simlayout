using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Interface for mapping between domain models and DTOs
    /// </summary>
    /// <typeparam name="TDomain">The domain model type</typeparam>
    public interface IElementMapper<TDomain> where TDomain : class
    {
        /// <summary>
        /// Gets the element type string for this mapper (e.g., "Conveyor", "EOTCrane")
        /// </summary>
        string ElementType { get; }

        /// <summary>
        /// Converts a domain model to an ElementDto
        /// </summary>
        /// <param name="domain">The domain model instance</param>
        /// <param name="layoutId">The layout ID this element belongs to</param>
        /// <returns>An ElementDto representing the domain model</returns>
        ElementDto ToDto(TDomain domain, string layoutId);

        /// <summary>
        /// Converts an ElementDto back to a domain model
        /// </summary>
        /// <param name="dto">The ElementDto to convert</param>
        /// <returns>A domain model instance</returns>
        TDomain FromDto(ElementDto dto);
    }
}
