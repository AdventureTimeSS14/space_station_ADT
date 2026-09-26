using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Drake;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Chasm;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake.Loot;

public sealed class ADTLavaStaffSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly ADTDrakeEffectsSystem _drakeEffects = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavaStaffComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTLavaStaffComponent, ADTLavaStaffDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<ADTLavaStaffComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        var now = _timing.CurTime;
        if (ent.Comp.NextUse > now)
            return;

        if (!TryGetTile(args.ClickLocation, out var gridUid, out var grid, out var tile))
            return;

        if (IsBanned(ent, gridUid, grid, tile))
            return;

        args.Handled = true;

        if (!HasComp<ADTLavalandMapComponent>(Transform(args.User).MapUid))
        {
            ent.Comp.NextUse = now + ent.Comp.FailCooldown;
            _popup.PopupEntity(Loc.GetString("adt-lava-staff-fail", ("staff", ent.Owner), ("user", args.User)), args.User, PopupType.MediumCaution);
            Spawn(ent.Comp.FailEffect, Transform(args.User).Coordinates);
            _audio.PlayPvs(ent.Comp.FailSound, args.User);
            return;
        }

        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        if (!_examine.InRangeUnOccluded(args.User, coords, ent.Comp.Range))
            return;

        if (FindLava(ent, gridUid, grid, tile) is { } lava)
        {
            ResetLava(ent, args.User, lava, gridUid, grid, tile);
            return;
        }

        var warning = Spawn(ent.Comp.WarningProto, coords);
        _popup.PopupEntity(Loc.GetString("adt-lava-staff-aim", ("staff", ent.Owner), ("user", args.User)), args.User, PopupType.MediumCaution);
        ent.Comp.NextUse = now + ent.Comp.CreateDelay + TimeSpan.FromSeconds(0.1);

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.CreateDelay,
            new ADTLavaStaffDoAfterEvent(GetNetCoordinates(coords), GetNetEntity(warning)),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ent.Comp.NextUse = now;
            QueueDel(warning);
        }
    }

    private void OnDoAfter(Entity<ADTLavaStaffComponent> ent, ref ADTLavaStaffDoAfterEvent args)
    {
        if (TryGetEntity(args.Warning, out var warning))
            QueueDel(warning);

        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            ent.Comp.NextUse = _timing.CurTime;
            return;
        }

        var coords = GetCoordinates(args.Location);
        if (!TryGetTile(coords, out var gridUid, out var grid, out var tile))
            return;

        if (FindLava(ent, gridUid, grid, tile) == null)
        {
            foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
            {
                if (HasComp<ChasmComponent>(anchored))
                    QueueDel(anchored);
            }

            Spawn(ent.Comp.LavaProto, _map.GridTileToLocal(gridUid, grid, tile));
        }

        _popup.PopupEntity(Loc.GetString("adt-lava-staff-create", ("user", args.User)), args.User, PopupType.MediumCaution);
        _adminLogger.Add(LogType.Tile, LogImpact.High, $"{ToPrettyString(args.User):user} fired the lava staff {ToPrettyString(ent):staff} at {coords}");
        ent.Comp.NextUse = _timing.CurTime + ent.Comp.CreateCooldown;
        _audio.PlayPvs(ent.Comp.UseSound, coords);
    }

    private void ResetLava(Entity<ADTLavaStaffComponent> ent, EntityUid user, EntityUid lava, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (TryComp<ADTDrakeTempLavaComponent>(lava, out var tempLava))
            _drakeEffects.RestoreReplaced((lava, tempLava));

        QueueDel(lava);

        var tileRef = _map.GetTileRef(gridUid, grid, tile);
        if (_tileDefMan.TryGetDefinition(ent.Comp.ResetTile, out var resetDef) && tileRef.Tile.TypeId != resetDef.TileId)
            _tile.ReplaceTile(tileRef, (ContentTileDefinition) resetDef, gridUid, grid);

        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        _popup.PopupEntity(Loc.GetString("adt-lava-staff-reset", ("user", user)), user, PopupType.MediumCaution);
        ent.Comp.NextUse = _timing.CurTime + ent.Comp.ResetCooldown;
        _audio.PlayPvs(ent.Comp.UseSound, coords);
    }

    private bool TryGetTile(EntityCoordinates coords, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        if (_transform.GetGrid(coords) is not { } gridEnt || !TryComp(gridEnt, out MapGridComponent? gridComp))
            return false;

        gridUid = gridEnt;
        grid = gridComp;
        tile = _map.TileIndicesFor(gridUid, grid, coords);

        return !_map.GetTileRef(gridUid, grid, tile).Tile.IsEmpty;
    }

    private bool IsBanned(Entity<ADTLavaStaffComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, anchored))
                return true;
        }

        return false;
    }

    private EntityUid? FindLava(Entity<ADTLavaStaffComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (MetaData(anchored).EntityPrototype is not { } proto)
                continue;

            foreach (var lava in ent.Comp.LavaPrototypes)
            {
                if (proto.ID == lava)
                    return anchored;
            }
        }

        return null;
    }
}
