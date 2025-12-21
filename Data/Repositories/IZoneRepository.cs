using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;

namespace LayoutEditor.Data.Repositories
{
    /// <summary>
    /// Repository interface for zone CRUD operations
    /// </summary>
    public interface IZoneRepository
    {
        /// <summary>
        /// Gets a zone by its ID
        /// </summary>
        Task<ZoneDto?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all zones for a specific layout
        /// </summary>
        Task<IEnumerable<ZoneDto>> GetByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Gets all zones of a specific type for a layout
        /// </summary>
        Task<IEnumerable<ZoneDto>> GetByLayoutAndTypeAsync(string layoutId, string zoneType);

        /// <summary>
        /// Inserts a new zone
        /// </summary>
        Task<bool> InsertAsync(ZoneDto zone);

        /// <summary>
        /// Updates an existing zone
        /// </summary>
        Task<bool> UpdateAsync(ZoneDto zone);

        /// <summary>
        /// Deletes a zone by ID
        /// </summary>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Deletes all zones for a specific layout
        /// </summary>
        Task<int> DeleteByLayoutIdAsync(string layoutId);

        /// <summary>
        /// Checks if a zone exists
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Gets the count of zones for a layout
        /// </summary>
        Task<int> GetCountAsync(string layoutId);

        /// <summary>
        /// Adds an element to a zone (ElementZones junction table)
        /// </summary>
        Task<bool> AddElementToZoneAsync(string zoneId, string elementId);

        /// <summary>
        /// Removes an element from a zone
        /// </summary>
        Task<bool> RemoveElementFromZoneAsync(string zoneId, string elementId);

        /// <summary>
        /// Gets all element IDs in a zone
        /// </summary>
        Task<IEnumerable<string>> GetElementsInZoneAsync(string zoneId);

        /// <summary>
        /// Gets all zone IDs containing an element
        /// </summary>
        Task<IEnumerable<string>> GetZonesForElementAsync(string elementId);
    }
}
