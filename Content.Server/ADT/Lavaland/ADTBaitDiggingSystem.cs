using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Burial.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTBaitDiggingSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(Vector2i, Tile)> _tiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShovelComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ShovelComponent, ADTBaitDigDoAfterEvent>(OnDigDoAfter);
        SubscribeLocalEvent<ADTRefillDugTilesComponent, StatusEffectAppliedEvent>(OnRefillWeather);
    }

    private void OnAfterInteract(Entity<ShovelComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (!TryGetDiggableTile(args.ClickLocation, out _, out var dig, out _))
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            dig.Comp.DigTime / ent.Comp.SpeedModifier,
            new ADTBaitDigDoAfterEvent(GetNetCoordinates(args.ClickLocation)),
            ent.Owner,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _audio.PlayPvs(dig.Comp.DigSound, args.ClickLocation);
        args.Handled = true;
    }

    private void OnDigDoAfter(Entity<ShovelComponent> ent, ref ADTBaitDigDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var coords = GetCoordinates(args.Location);

        if (!TryGetDiggableTile(coords, out var grid, out var dig, out var indices))
            return;

        if (!_tileDefs.TryGetDefinition(dig.Comp.DugTile.Id, out var dugDef))
            return;

        _map.SetTile(grid.Owner, grid.Comp, indices, new Tile(dugDef.TileId));
        dig.Comp.DugTiles.Add(indices);

        if (!_random.Prob(dig.Comp.Chance))
        {
            _popup.PopupEntity(Loc.GetString("adt-bait-dig-nothing"), args.User, args.User);
            return;
        }

        var spawnCoords = _map.GridTileToLocal(grid.Owner, grid.Comp, indices);

        foreach (var proto in _entityTable.GetSpawns(dig.Comp.Table))
        {
            var bait = Spawn(proto, spawnCoords);
            _popup.PopupEntity(Loc.GetString("adt-bait-dig-found", ("bait", bait)), args.User, args.User);
        }
    }

    private void OnRefillWeather(Entity<ADTRefillDugTilesComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<ADTBaitDiggingComponent>(args.Target, out var dig) ||
            !TryComp<MapGridComponent>(args.Target, out var grid))
        {
            return;
        }

        if (dig.DugTiles.Count == 0)
            return;

        if (!_tileDefs.TryGetDefinition(dig.SourceTile.Id, out var sourceDef))
            return;

        _tiles.Clear();

        foreach (var indices in dig.DugTiles)
        {
            if (!_map.TryGetTileRef(args.Target, grid, indices, out var tileRef))
                continue;

            if (_tileDefs[tileRef.Tile.TypeId].ID != dig.DugTile.Id)
                continue;

            _tiles.Add((indices, new Tile(sourceDef.TileId)));
        }

        dig.DugTiles.Clear();
        _map.SetTiles(args.Target, grid, _tiles);
    }

    private bool TryGetDiggableTile(
        EntityCoordinates coords,
        out Entity<MapGridComponent> grid,
        out Entity<ADTBaitDiggingComponent> dig,
        out Vector2i indices)
    {
        grid = default;
        dig = default;
        indices = default;

        if (_transform.GetGrid(coords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp) ||
            !TryComp<ADTBaitDiggingComponent>(gridUid, out var digComp))
        {
            return false;
        }

        indices = _map.TileIndicesFor(gridUid, gridComp, coords);

        if (!_map.TryGetTileRef(gridUid, gridComp, indices, out var tileRef))
            return false;

        if (_tileDefs[tileRef.Tile.TypeId].ID != digComp.SourceTile.Id)
            return false;

        grid = (gridUid, gridComp);
        dig = (gridUid, digComp);
        return true;
    }
}
