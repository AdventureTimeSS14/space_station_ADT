using Content.Server.ADT.Generation;
using Content.Server.ADT.Planet.RestrictedZone;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Parallax;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Planet;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using System.Numerics;
using System.Linq;
using Robust.Shared.Utility;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Server.ADT.Planet;

public sealed class PlanetSystem : EntitySystem
{
    [Dependency] private readonly ADTLavalandGenerationSystem _lavaland = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private List<(Vector2i, Tile)> _setTiles = new();

    /// <summary>
    /// Spawn a planet map from a planet prototype.
    /// </summary>
    public EntityUid SpawnPlanet(ProtoId<PlanetPrototype> id, bool runMapInit = true)
    {
        var planet = _proto.Index(id);

        var map = _map.CreateMap(out _, runMapInit: runMapInit);

        var biomeEnabled = _configManager.GetCVar(ADTCCVars.BiomeGenerationEnabled);
        if (biomeEnabled)
        {
            _biome.EnsurePlanet(map, _proto.Index(planet.Biome), mapLight: planet.MapLight, dayCycle: planet.DayCycle);
        }

        if (biomeEnabled)
        {
            var biome = Comp<BiomeComponent>(map);
            foreach (var layer in planet.BiomeMarkerLayers)
            {
                _biome.AddMarkerLayer(map, biome, layer);
            }
        }

        if (planet.AddedComponents is { } added)
            EntityManager.AddComponents(map, added);

        _atmos.SetMapAtmosphere(map, false, planet.Atmosphere);

        _meta.SetEntityName(map, Loc.GetString(planet.MapName));

        return map;
    }

    /// <summary>
    /// Spawns an initialized planet map from a planet prototype and loads a grid onto it.
    /// Returns the map entity if loading succeeded.
    /// </summary>
    public EntityUid? LoadPlanet(ProtoId<PlanetPrototype> id, string path)
    {
        var map = SpawnPlanet(id, runMapInit: false);
        var mapId = Comp<MapComponent>(map).MapId;

        if (!_mapLoader.TryLoadGrid(mapId, new ResPath(path), out var grids))
        {
            Log.Error($"Failed to load planet grid {path} for planet {id}!");
            return null;
        }

        if (grids.HasValue)
        {
            var gridUid = grids.Value;
            _setTiles.Clear();
            var aabb = Comp<MapGridComponent>(gridUid).LocalAABB;
            _biome.ReserveTiles(map, aabb.Enlarged(0.2f), _setTiles);

            var center = aabb.Center;
            ApplyRestrictedZone(map, center);

            if (TryComp<ADTLavalandGenerationComponent>(map, out var generation))
            {
                generation.BaseCenter = center;
                _lavaland.ReserveSafeZone((map, generation), center);

                if (generation.BaseBeacon is { } beacon)
                    Spawn(beacon, new EntityCoordinates(gridUid, center));
            }

            if (TryComp<ADTLavalandPopulationComponent>(map, out var population))
                population.BaseCenter = center;

            if (TryComp<ADTMegafaunaSpawnComponent>(map, out var megafauna))
                megafauna.BaseCenter = center;
        }
        else
        {
            Log.Error("Grid not found for this map.");
        }

        _map.InitializeMap(map);
        return map;
    }

    private void ApplyRestrictedZone(EntityUid map, Vector2 center)
    {
        if (!TryComp<ADTRestrictedZoneComponent>(map, out var zone) ||
            !TryComp<RestrictedRangeComponent>(map, out var restricted))
        {
            return;
        }

        restricted.Origin = center;
        Dirty(map, restricted);

        var limit = MathF.Max(0f, restricted.Range - zone.SpawnBuffer);

        if (TryComp<ADTLavalandGenerationComponent>(map, out var generation))
        {
            generation.MaxRadius = MathF.Min(generation.MaxRadius, limit);
            generation.MinRadius = MathF.Min(generation.MinRadius, generation.MaxRadius);

            foreach (var group in generation.Groups)
            {
                group.MinDistanceFromCenter = MathF.Min(group.MinDistanceFromCenter, generation.MaxRadius);
            }
        }

        if (TryComp<ADTLavalandPopulationComponent>(map, out var population))
        {
            foreach (var group in population.Groups)
            {
                group.MaxRadius = MathF.Min(group.MaxRadius, limit);
                group.MinDistanceFromCenter = MathF.Min(group.MinDistanceFromCenter, group.MaxRadius);
            }
        }

        if (TryComp<ADTMegafaunaSpawnComponent>(map, out var megafauna))
        {
            megafauna.MaxRadius = MathF.Min(megafauna.MaxRadius, limit);
            megafauna.MinDistanceFromCenter = MathF.Min(megafauna.MinDistanceFromCenter, megafauna.MaxRadius);
        }
    }
}
