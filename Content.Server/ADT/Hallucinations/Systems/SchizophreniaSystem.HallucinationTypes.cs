using System.Linq;
using System.Numerics;
using Content.Server.ADT.Hallucinations.Types;
using Content.Shared.ADT.Hallucinations.Events;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Hallucinations.Systems;

public sealed partial class SchizophreniaSystem : EntitySystem
{
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private void Perform(EntityUid uid, BaseHallucinationsType type)
    {
        switch (type)
        {
            case MobHallucinations mob:
                PerformMob(uid, mob);
                break;
            case AppearanceHallucinations appearance:
                PerformAppearance(uid, appearance);
                break;
            default:
                break;
        }
    }

    private void PerformMob(EntityUid uid, MobHallucinations mob)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
            return;

        var worldPos = _xform.GetMapCoordinates(uid).Position;

        if (!TryGetValidTiles((xform.GridUid.Value, mapGrid), worldPos, mob, out var tiles))
            return;

        var count = Math.Min(mob.SpawnCount.Next(_random), tiles.Count);

        for (var i = 0; i < count; i++)
        {
            var tile = _random.Pick(tiles);

            var ent = Spawn(_random.Pick(mob.Entities), new EntityCoordinates(tile.GridUid, tile.GridIndices + mapGrid.TileSizeHalfVector));
            AddAsHallucination(uid, ent);

            tiles.Remove(tile);
        }
    }

    private bool TryGetValidTiles(Entity<MapGridComponent> grid, Vector2 source, MobHallucinations mob, out List<TileRef> tiles)
    {
        var exceptTiles = _map.GetTilesIntersecting(grid.Owner, grid.Comp, new Circle(source, mob.Range.Min));
        tiles = _map.GetTilesIntersecting(grid.Owner, grid.Comp, new Circle(source, mob.Range.Max)).Except(exceptTiles).ToList();

        if (tiles.Count <= 0)
            return false;

        for (var i = tiles.Count() - 1; i >= 0; i--)
        {
            var item = tiles[i];
            var ents = _lookup.GetEntitiesInTile(item);

            if (ents.Count <= 0 && mob.Whitelist != null)
            {
                tiles.RemoveAt(i);
                break;
            }

            foreach (var ent in ents)
            {
                if (!_whitelist.IsWhitelistPassOrNull(mob.Whitelist, ent) ||
                    _whitelist.IsWhitelistPass(mob.Blacklist, ent))
                {
                    tiles.RemoveAt(i);
                    break;
                }
            }
        }

        return tiles.Count > 0;
    }

    private void PerformAppearance(EntityUid uid, AppearanceHallucinations appearance)
    {
        var selected = _random.Pick(appearance.Appearances);
        RaiseNetworkEvent(new SetHallucinationAppearanceMessage(selected), uid);
    }
}
