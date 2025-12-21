using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// Repository interface for layout CRUD operations
    /// </summary>
    public interface ILayoutRepository
    {
        /// <summary>
        /// Gets a layout by its ID
        /// </summary>
        Task<LayoutDto?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all layouts
        /// </summary>
        Task<IEnumerable<LayoutDto>> GetAllAsync();

        /// <summary>
        /// Inserts a new layout
        /// </summary>
        Task<bool> InsertAsync(LayoutDto layout);

        /// <summary>
        /// Updates an existing layout
        /// </summary>
        Task<bool> UpdateAsync(LayoutDto layout);

        /// <summary>
        /// Deletes a layout by ID
        /// </summary>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Checks if a layout exists
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Gets the total count of layouts
        /// </summary>
        Task<int> GetCountAsync();
    }
}
