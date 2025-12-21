using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// Repository interface for connection CRUD operations
    /// </summary>
    public interface IConnectionRepository
    {
        /// <summary>
        /// Gets a connection by its ID
        /// </summary>
        Task<ConnectionDto?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all connections for a specific layout
        /// </summary>
        Task<IEnumerable<ConnectionDto>> GetByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Gets all connections of a specific type for a layout
        /// </summary>
        Task<IEnumerable<ConnectionDto>> GetByLayoutAndTypeAsync(string layoutId, string connectionType);

        /// <summary>
        /// Gets all connections from a specific source element
        /// </summary>
        Task<IEnumerable<ConnectionDto>> GetBySourceElementAsync(string sourceElementId);

        /// <summary>
        /// Gets all connections to a specific target element
        /// </summary>
        Task<IEnumerable<ConnectionDto>> GetByTargetElementAsync(string targetElementId);

        /// <summary>
        /// Gets connections between two specific elements
        /// </summary>
        Task<IEnumerable<ConnectionDto>> GetBetweenElementsAsync(string sourceElementId, string targetElementId);

        /// <summary>
        /// Inserts a new connection
        /// </summary>
        Task<bool> InsertAsync(ConnectionDto connection);

        /// <summary>
        /// Updates an existing connection
        /// </summary>
        Task<bool> UpdateAsync(ConnectionDto connection);

        /// <summary>
        /// Deletes a connection by ID
        /// </summary>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Deletes all connections for a specific layout
        /// </summary>
        Task<int> DeleteByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Checks if a connection exists
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Gets the count of connections for a layout
        /// </summary>
        Task<int> GetCountAsync(string layoutId);

        /// <summary>
        /// Batch insert multiple connections
        /// </summary>
        Task<int> BatchInsertAsync(IEnumerable<ConnectionDto> connections);
    }
}
