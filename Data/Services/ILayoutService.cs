using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Services
{
    /// <summary>
    /// High-level service for managing complete layout persistence
    /// Coordinates repositories and mappers to save/load entire layouts
    /// </summary>
    public interface ILayoutService
    {
        /// <summary>
        /// Saves a complete layout with all its elements, connections, and zones
        /// </summary>
        /// <param name="layout">The layout to save</param>
        /// <returns>True if save was successful</returns>
        Task<bool> SaveLayoutAsync(LayoutData layout);

        /// <summary>
        /// Loads a complete layout with all its elements, connections, and zones
        /// </summary>
        /// <param name="layoutId">The layout ID to load</param>
        /// <returns>The loaded layout, or null if not found</returns>
        Task<LayoutData?> LoadLayoutAsync(string layoutId);

        /// <summary>
        /// Deletes a layout and all its associated data
        /// </summary>
        /// <param name="layoutId">The layout ID to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteLayoutAsync(string layoutId);

        /// <summary>
        /// Gets metadata for all layouts (without loading full element data)
        /// </summary>
        /// <returns>List of layout metadata</returns>
        Task<IEnumerable<LayoutMetadataDto>> GetAllLayoutMetadataAsync();

        /// <summary>
        /// Checks if a layout exists
        /// </summary>
        /// <param name="layoutId">The layout ID to check</param>
        /// <returns>True if layout exists</returns>
        Task<bool> LayoutExistsAsync(string layoutId);

        /// <summary>
        /// Gets the count of elements in a layout
        /// </summary>
        /// <param name="layoutId">The layout ID</param>
        /// <returns>Number of elements in the layout</returns>
        Task<int> GetElementCountAsync(string layoutId);
    }
}
