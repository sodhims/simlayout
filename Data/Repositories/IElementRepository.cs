using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// Repository interface for CRUD operations on layout elements
    /// </summary>
    public interface IElementRepository
    {
        /// <summary>
        /// Gets an element by its ID
        /// </summary>
        Task<ElementDto?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all elements for a specific layout
        /// </summary>
        Task<IEnumerable<ElementDto>> GetByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Gets all elements of a specific type for a layout
        /// </summary>
        Task<IEnumerable<ElementDto>> GetByLayoutAndTypeAsync(string layoutId, string elementType);

        /// <summary>
        /// Gets all elements on a specific layer for a layout
        /// </summary>
        Task<IEnumerable<ElementDto>> GetByLayoutAndLayerAsync(string layoutId, int layer);

        /// <summary>
        /// Inserts a new element
        /// </summary>
        Task<bool> InsertAsync(ElementDto element);

        /// <summary>
        /// Updates an existing element
        /// </summary>
        Task<bool> UpdateAsync(ElementDto element);

        /// <summary>
        /// Deletes an element by ID
        /// </summary>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Deletes all elements for a specific layout
        /// </summary>
        Task<int> DeleteByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Checks if an element exists
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Gets the count of elements for a layout
        /// </summary>
        Task<int> GetCountAsync(string layoutId);

        /// <summary>
        /// Batch insert multiple elements (for performance)
        /// </summary>
        Task<int> BatchInsertAsync(IEnumerable<ElementDto> elements);

        /// <summary>
        /// Batch update multiple elements (for performance)
        /// </summary>
        Task<int> BatchUpdateAsync(IEnumerable<ElementDto> elements);
    }
}
