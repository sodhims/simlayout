using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Data.Mapping;
using LayoutEditor.Data.Repositories;
using LayoutEditor.Models;
using Microsoft.Data.Sqlite;

namespace LayoutEditor.Data.Services
{
    /// <summary>
    /// High-level service for managing complete layout persistence
    /// Orchestrates repositories and mappers to save/load entire layouts
    /// </summary>
    public class LayoutService : ILayoutService
    {
        private readonly DatabaseManager _dbManager;
        private readonly ILayoutRepository _layoutRepo;
        private readonly IElementRepository _elementRepo;
        private readonly IConnectionRepository _connectionRepo;
        private readonly IZoneRepository _zoneRepo;
        private readonly MapperRegistry _mapperRegistry;

        public LayoutService(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _layoutRepo = new SQLiteLayoutRepository(dbManager);
            _elementRepo = new SQLiteElementRepository(dbManager);
            _connectionRepo = new SQLiteConnectionRepository(dbManager);
            _zoneRepo = new SQLiteZoneRepository(dbManager);
            _mapperRegistry = new MapperRegistry();
        }

        /// <summary>
        /// Saves a complete layout with all its elements
        /// </summary>
        public async Task<bool> SaveLayoutAsync(LayoutData layout)
        {
            if (layout == null || layout.Metadata == null)
                return false;

            try
            {
                // Save layout metadata
                var layoutDto = new LayoutDto
                {
                    Id = layout.Id,
                    Name = layout.Metadata.Name,
                    Width = layout.Canvas.Width,
                    Height = layout.Canvas.Height,
                    Unit = layout.Metadata.Units,
                    CreatedDate = layout.Metadata.Created,
                    ModifiedDate = layout.Metadata.Modified,
                    Version = 1 // Default version
                };

                bool layoutExists = await _layoutRepo.ExistsAsync(layoutDto.Id);
                bool layoutSaved = layoutExists
                    ? await _layoutRepo.UpdateAsync(layoutDto)
                    : await _layoutRepo.InsertAsync(layoutDto);

                if (!layoutSaved)
                    return false;

                // Delete existing elements for this layout (for update scenario)
                if (layoutExists)
                {
                    await _elementRepo.DeleteByLayoutIdAsync(layoutDto.Id);
                }

                // Save all elements using mappers
                await SaveElementsAsync(layout, layoutDto.Id);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Saves all elements from the layout
        /// </summary>
        private async Task SaveElementsAsync(LayoutData layout, string layoutId)
        {
            // Save conveyors
            foreach (var conveyor in layout.Conveyors)
            {
                var dto = _mapperRegistry.ConveyorToDto(conveyor, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save EOT cranes
            foreach (var crane in layout.EOTCranes)
            {
                var dto = _mapperRegistry.EOTCraneToDto(crane, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save AGV paths
            foreach (var path in layout.AGVPaths)
            {
                var dto = _mapperRegistry.AGVPathToDto(path, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save walkways
            foreach (var walkway in layout.Walkways)
            {
                var dto = _mapperRegistry.WalkwayToDto(walkway, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save pedestrian crossings
            foreach (var crossing in layout.PedestrianCrossings)
            {
                var dto = _mapperRegistry.PedestrianCrossingToDto(crossing, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save safety zones
            foreach (var zone in layout.SafetyZones)
            {
                var dto = _mapperRegistry.SafetyZoneToDto(zone, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save conflict resolutions
            foreach (var resolution in layout.ConflictResolutions)
            {
                var dto = _mapperRegistry.ConflictResolutionToDto(resolution, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Save layer connections
            foreach (var connection in layout.LayerConnections)
            {
                var dto = _mapperRegistry.LayerConnectionToDto(connection, layoutId);
                await _elementRepo.InsertAsync(dto);
            }

            // Note: Additional element types can be added here as mappers are created
        }

        /// <summary>
        /// Loads a complete layout with all its elements
        /// </summary>
        public async Task<LayoutData?> LoadLayoutAsync(string layoutId)
        {
            var layoutDto = await _layoutRepo.GetByIdAsync(layoutId);
            if (layoutDto == null)
                return null;

            var layout = new LayoutData();

            // Load layout metadata
            layout.Id = layoutDto.Id;
            layout.Metadata.Name = layoutDto.Name;
            layout.Metadata.Units = layoutDto.Unit;
            layout.Metadata.Created = layoutDto.CreatedDate;
            layout.Metadata.Modified = layoutDto.ModifiedDate;
            layout.Canvas.Width = layoutDto.Width;
            layout.Canvas.Height = layoutDto.Height;

            // Load all elements
            var elements = await _elementRepo.GetByLayoutIdAsync(layoutId);
            foreach (var elementDto in elements)
            {
                LoadElement(layout, elementDto);
            }

            return layout;
        }

        /// <summary>
        /// Loads a single element into the layout based on its type
        /// </summary>
        private void LoadElement(LayoutData layout, ElementDto dto)
        {
            switch (dto.ElementType)
            {
                case DbElementType.Conveyor:
                    var conveyor = _mapperRegistry.ConveyorFromDto(dto);
                    if (conveyor != null)
                        layout.Conveyors.Add(conveyor);
                    break;

                case DbElementType.EOTCrane:
                    var crane = _mapperRegistry.EOTCraneFromDto(dto);
                    if (crane != null)
                        layout.EOTCranes.Add(crane);
                    break;

                case DbElementType.AGVPath:
                    var path = _mapperRegistry.AGVPathFromDto(dto);
                    if (path != null)
                        layout.AGVPaths.Add(path);
                    break;

                case DbElementType.Walkway:
                    var walkway = _mapperRegistry.WalkwayFromDto(dto);
                    if (walkway != null)
                        layout.Walkways.Add(walkway);
                    break;

                case DbElementType.PedestrianCrossing:
                    var crossing = _mapperRegistry.PedestrianCrossingFromDto(dto);
                    if (crossing != null)
                        layout.PedestrianCrossings.Add(crossing);
                    break;

                case DbElementType.SafetyZone:
                    var zone = _mapperRegistry.SafetyZoneFromDto(dto);
                    if (zone != null)
                        layout.SafetyZones.Add(zone);
                    break;

                case DbElementType.ConflictResolution:
                    var resolution = _mapperRegistry.ConflictResolutionFromDto(dto);
                    if (resolution != null)
                        layout.ConflictResolutions.Add(resolution);
                    break;

                case DbElementType.LayerConnection:
                    var connection = _mapperRegistry.LayerConnectionFromDto(dto);
                    if (connection != null)
                        layout.LayerConnections.Add(connection);
                    break;

                // Additional element types can be added here
            }
        }

        /// <summary>
        /// Deletes a layout and all its associated data
        /// </summary>
        public async Task<bool> DeleteLayoutAsync(string layoutId)
        {
            // CASCADE DELETE will handle elements, connections, and zones automatically
            return await _layoutRepo.DeleteAsync(layoutId);
        }

        /// <summary>
        /// Gets metadata for all layouts without loading full element data
        /// </summary>
        public async Task<IEnumerable<LayoutMetadataDto>> GetAllLayoutMetadataAsync()
        {
            var layouts = await _layoutRepo.GetAllAsync();
            var metadataList = new List<LayoutMetadataDto>();

            foreach (var layout in layouts)
            {
                var elementCount = await _elementRepo.GetCountAsync(layout.Id);

                metadataList.Add(new LayoutMetadataDto
                {
                    Id = layout.Id,
                    Name = layout.Name,
                    Width = layout.Width,
                    Height = layout.Height,
                    Unit = layout.Unit,
                    CreatedDate = layout.CreatedDate,
                    ModifiedDate = layout.ModifiedDate,
                    Version = layout.Version,
                    ElementCount = elementCount
                });
            }

            return metadataList;
        }

        /// <summary>
        /// Checks if a layout exists
        /// </summary>
        public async Task<bool> LayoutExistsAsync(string layoutId)
        {
            return await _layoutRepo.ExistsAsync(layoutId);
        }

        /// <summary>
        /// Gets the count of elements in a layout
        /// </summary>
        public async Task<int> GetElementCountAsync(string layoutId)
        {
            return await _elementRepo.GetCountAsync(layoutId);
        }
    }
}
