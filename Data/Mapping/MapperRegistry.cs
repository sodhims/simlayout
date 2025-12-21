using System;
using System.Collections.Generic;
using LayoutEditor.Data.DTOs;
using LayoutEditor.Models;

namespace LayoutEditor.Data.Mapping
{
    /// <summary>
    /// Registry for managing element mappers
    /// </summary>
    public class MapperRegistry
    {
        private readonly Dictionary<string, object> _mappers = new();

        public MapperRegistry()
        {
            // Register all mappers
            RegisterMapper(new ConveyorMapper());
            RegisterMapper(new EOTCraneMapper());
            RegisterMapper(new AGVPathMapper());

            // Register pedestrian mappers
            RegisterMapper(new WalkwayMapper());
            RegisterMapper(new PedestrianCrossingMapper());
            RegisterMapper(new SafetyZoneMapper());

            // Register conflict mappers
            RegisterMapper(new ConflictResolutionMapper());
            RegisterMapper(new LayerConnectionMapper());
        }

        /// <summary>
        /// Registers a mapper for a specific element type
        /// </summary>
        public void RegisterMapper<TDomain>(IElementMapper<TDomain> mapper) where TDomain : class
        {
            _mappers[mapper.ElementType] = mapper;
        }

        /// <summary>
        /// Gets a mapper for a specific element type
        /// </summary>
        public IElementMapper<TDomain>? GetMapper<TDomain>(string elementType) where TDomain : class
        {
            if (_mappers.TryGetValue(elementType, out var mapper))
            {
                return mapper as IElementMapper<TDomain>;
            }
            return null;
        }

        /// <summary>
        /// Converts a domain model to DTO using the appropriate mapper
        /// </summary>
        public ElementDto? ToDto<TDomain>(TDomain domain, string layoutId, string elementType) where TDomain : class
        {
            var mapper = GetMapper<TDomain>(elementType);
            return mapper?.ToDto(domain, layoutId);
        }

        /// <summary>
        /// Converts a DTO to domain model using the appropriate mapper
        /// </summary>
        public TDomain? FromDto<TDomain>(ElementDto dto) where TDomain : class
        {
            var mapper = GetMapper<TDomain>(dto.ElementType);
            return mapper?.FromDto(dto);
        }

        /// <summary>
        /// Converts a ConveyorData to DTO
        /// </summary>
        public ElementDto ConveyorToDto(ConveyorData conveyor, string layoutId)
        {
            var mapper = GetMapper<ConveyorData>(DbElementType.Conveyor);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.Conveyor}");
            return mapper.ToDto(conveyor, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to ConveyorData
        /// </summary>
        public ConveyorData ConveyorFromDto(ElementDto dto)
        {
            var mapper = GetMapper<ConveyorData>(DbElementType.Conveyor);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.Conveyor}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts an EOTCraneData to DTO
        /// </summary>
        public ElementDto EOTCraneToDto(EOTCraneData crane, string layoutId)
        {
            var mapper = GetMapper<EOTCraneData>(DbElementType.EOTCrane);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.EOTCrane}");
            return mapper.ToDto(crane, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to EOTCraneData
        /// </summary>
        public EOTCraneData EOTCraneFromDto(ElementDto dto)
        {
            var mapper = GetMapper<EOTCraneData>(DbElementType.EOTCrane);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.EOTCrane}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts an AGVPathData to DTO
        /// </summary>
        public ElementDto AGVPathToDto(AGVPathData path, string layoutId)
        {
            var mapper = GetMapper<AGVPathData>(DbElementType.AGVPath);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.AGVPath}");
            return mapper.ToDto(path, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to AGVPathData
        /// </summary>
        public AGVPathData AGVPathFromDto(ElementDto dto)
        {
            var mapper = GetMapper<AGVPathData>(DbElementType.AGVPath);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.AGVPath}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts a WalkwayData to DTO
        /// </summary>
        public ElementDto WalkwayToDto(WalkwayData walkway, string layoutId)
        {
            var mapper = GetMapper<WalkwayData>(DbElementType.Walkway);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.Walkway}");
            return mapper.ToDto(walkway, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to WalkwayData
        /// </summary>
        public WalkwayData WalkwayFromDto(ElementDto dto)
        {
            var mapper = GetMapper<WalkwayData>(DbElementType.Walkway);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.Walkway}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts a PedestrianCrossingData to DTO
        /// </summary>
        public ElementDto PedestrianCrossingToDto(PedestrianCrossingData crossing, string layoutId)
        {
            var mapper = GetMapper<PedestrianCrossingData>(DbElementType.PedestrianCrossing);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.PedestrianCrossing}");
            return mapper.ToDto(crossing, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to PedestrianCrossingData
        /// </summary>
        public PedestrianCrossingData PedestrianCrossingFromDto(ElementDto dto)
        {
            var mapper = GetMapper<PedestrianCrossingData>(DbElementType.PedestrianCrossing);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.PedestrianCrossing}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts a SafetyZoneData to DTO
        /// </summary>
        public ElementDto SafetyZoneToDto(SafetyZoneData zone, string layoutId)
        {
            var mapper = GetMapper<SafetyZoneData>(DbElementType.SafetyZone);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.SafetyZone}");
            return mapper.ToDto(zone, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to SafetyZoneData
        /// </summary>
        public SafetyZoneData SafetyZoneFromDto(ElementDto dto)
        {
            var mapper = GetMapper<SafetyZoneData>(DbElementType.SafetyZone);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.SafetyZone}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts a ConflictResolutionData to DTO
        /// </summary>
        public ElementDto ConflictResolutionToDto(ConflictResolutionData resolution, string layoutId)
        {
            var mapper = GetMapper<ConflictResolutionData>(DbElementType.ConflictResolution);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.ConflictResolution}");
            return mapper.ToDto(resolution, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to ConflictResolutionData
        /// </summary>
        public ConflictResolutionData ConflictResolutionFromDto(ElementDto dto)
        {
            var mapper = GetMapper<ConflictResolutionData>(DbElementType.ConflictResolution);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.ConflictResolution}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Converts a LayerConnectionData to DTO
        /// </summary>
        public ElementDto LayerConnectionToDto(LayerConnectionData connection, string layoutId)
        {
            var mapper = GetMapper<LayerConnectionData>(DbElementType.LayerConnection);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.LayerConnection}");
            return mapper.ToDto(connection, layoutId);
        }

        /// <summary>
        /// Converts an ElementDto to LayerConnectionData
        /// </summary>
        public LayerConnectionData LayerConnectionFromDto(ElementDto dto)
        {
            var mapper = GetMapper<LayerConnectionData>(DbElementType.LayerConnection);
            if (mapper == null)
                throw new InvalidOperationException($"No mapper registered for {DbElementType.LayerConnection}");
            return mapper.FromDto(dto);
        }

        /// <summary>
        /// Checks if a mapper is registered for an element type
        /// </summary>
        public bool HasMapper(string elementType)
        {
            return _mappers.ContainsKey(elementType);
        }

        /// <summary>
        /// Gets all registered element types
        /// </summary>
        public IEnumerable<string> GetRegisteredTypes()
        {
            return _mappers.Keys;
        }
    }
}
