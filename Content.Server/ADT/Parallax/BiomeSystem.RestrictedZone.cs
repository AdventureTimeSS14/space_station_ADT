using System.Numerics;
using Content.Server.ADT.Planet.RestrictedZone;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private readonly Dictionary<BiomeComponent, RestrictedZoneData> _restrictedZones = new();

    private void CacheRestrictedZones()
    {
        _restrictedZones.Clear();

        var query = EntityQueryEnumerator<ADTRestrictedZoneComponent, RestrictedRangeComponent, BiomeComponent>();

        while (query.MoveNext(out _, out var zone, out var range, out var biome))
        {
            _restrictedZones[biome] = new RestrictedZoneData(
                range.Origin,
                range.Range + zone.TerrainMargin,
                range.Range + zone.MarkerMargin);
        }
    }

    private bool IsChunkAllowed(BiomeComponent biome, Vector2i origin, int size, bool marker)
    {
        if (!_restrictedZones.TryGetValue(biome, out var zone))
            return true;

        var range = marker ? zone.MarkerRange : zone.TerrainRange;
        var min = new Vector2(origin.X, origin.Y);
        var closest = Vector2.Clamp(zone.Origin, min, min + new Vector2(size, size));

        return (closest - zone.Origin).LengthSquared() <= range * range;
    }

    private bool IsMarkerNodeAllowed(BiomeComponent biome, Vector2i node)
    {
        if (!_restrictedZones.TryGetValue(biome, out var zone))
            return true;

        var position = new Vector2(node.X + 0.5f, node.Y + 0.5f);

        return (position - zone.Origin).LengthSquared() <= zone.MarkerRange * zone.MarkerRange;
    }

    private readonly record struct RestrictedZoneData(Vector2 Origin, float TerrainRange, float MarkerRange);
}
