using System.Linq;
using Content.Shared.ADT.Fishing;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Fishing;

public sealed class ADTFishingSystem : EntitySystem
{
    [Dependency] private readonly ADTFishingMinigameSystem _minigame = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTFishingRodComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTFishingRodComponent, ADTFishingCastActionEvent>(OnCastAction);
        SubscribeLocalEvent<ADTFishingRodComponent, ADTFishingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ADTFishingRodComponent, ComponentShutdown>(OnRodShutdown);
    }

    private void OnAfterInteract(Entity<ADTFishingRodComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<ADTFishingSpotComponent>(target, out var spot))
            return;

        args.Handled = true;
        TryStartFishing(ent, args.User, (target, spot));
    }

    private void OnCastAction(Entity<ADTFishingRodComponent> ent, ref ADTFishingCastActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var userPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var dist = (userPos.Position - targetPos.Position).Length();

        if (userPos.MapId != targetPos.MapId || dist > ent.Comp.CastRange)
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-too-far"), ent.Owner, args.Performer);
            return;
        }

        if (!TryGetSpotAt(args.Target, out var spot))
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-no-spot"), ent.Owner, args.Performer);
            return;
        }

        TryStartFishing(ent, args.Performer, spot);
    }

    private bool TryGetSpotAt(EntityCoordinates coords, out Entity<ADTFishingSpotComponent> spot)
    {
        spot = default;

        if (_transform.GetGrid(coords) is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, coords);
        foreach (var uid in _map.GetAnchoredEntities(gridUid, grid, indices))
        {
            if (!TryComp<ADTFishingSpotComponent>(uid, out var spotComp))
                continue;

            spot = (uid, spotComp);
            return true;
        }

        return false;
    }

    private void TryStartFishing(Entity<ADTFishingRodComponent> ent, EntityUid user, Entity<ADTFishingSpotComponent> spot)
    {

        if (!spot.Comp.CanBeFished || (spot.Comp.LavalandOnly && !IsLavaland(spot.Owner)))
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-nothing-lives"), ent.Owner, user);
            return;
        }

        if (ent.Comp.ActiveBobber != null || spot.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-already-fishing"), ent.Owner, user);
            return;
        }

        if (TryComp<WieldableComponent>(ent.Owner, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-needs-wield"), ent.Owner, user);
            return;
        }

        if (spot.Comp.RequiresBait && GetBait(ent) == null)
        {
            _popup.PopupEntity(Loc.GetString("adt-fishing-needs-bait"), ent.Owner, user);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.CastTime, new ADTFishingDoAfterEvent(), ent.Owner, spot.Owner, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            RequireCanInteract = false,
            DistanceThreshold = null,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        ent.Comp.ActiveBobber = Spawn(ent.Comp.Bobber, Transform(spot.Owner).Coordinates);
        ent.Comp.ActiveSpot = spot.Owner;
        spot.Comp.Occupied = true;

        _audio.PlayPvs(ent.Comp.ThrowSound, ent.Owner);
        var start = IsDeep(spot) ? "adt-fishing-start-deep" : "adt-fishing-start";
        _popup.PopupEntity(Loc.GetString(start), ent.Owner, user);
    }

    private void OnDoAfter(Entity<ADTFishingRodComponent> ent, ref ADTFishingDoAfterEvent args)
    {

        if (args.Target is not { } target || !TryComp<ADTFishingSpotComponent>(target, out var spotComp))
        {
            EndCast(ent, null);
            return;
        }

        if (args.Cancelled)
        {
            EndCast(ent, target);
            _popup.PopupEntity(Loc.GetString("adt-fishing-interrupted"), ent.Owner, args.User);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var bait = GetBait(ent);

        if (spotComp.RequiresBait && bait == null)
        {
            EndCast(ent, target);
            _popup.PopupEntity(Loc.GetString("adt-fishing-needs-bait"), ent.Owner, args.User);
            return;
        }

        EntProtoId? baitProto = null;

        if (bait != null)
        {
            var baitId = Prototype(bait.Value)?.ID;

            if (baitId != null)
                baitProto = new EntProtoId(baitId);

            QueueDel(bait.Value);
        }

        if (_random.Prob(ent.Comp.LoseChance))
        {
            EndCast(ent, target);
            _popup.PopupEntity(Loc.GetString("adt-fishing-lost-bait"), ent.Owner, args.User, PopupType.MediumCaution);
            return;
        }

        if (PickFish((target, spotComp), baitProto, ent.Comp.FavoriteBaitChance) is not { } fishProto)
        {
            EndCast(ent, target);
            return;
        }

        _minigame.Start(ent, args.User, (target, spotComp), fishProto);
    }

    private void OnRodShutdown(Entity<ADTFishingRodComponent> ent, ref ComponentShutdown args)
    {
        EndCast(ent, null);
    }

    public void EndCast(Entity<ADTFishingRodComponent> ent, EntityUid? spot)
    {
        if (ent.Comp.ActiveBobber is { } bobber)
        {
            QueueDel(bobber);
            ent.Comp.ActiveBobber = null;
        }

        spot ??= ent.Comp.ActiveSpot;
        if (TryComp<ADTFishingSpotComponent>(spot, out var spotComp))
            spotComp.Occupied = false;

        ent.Comp.ActiveSpot = null;
    }

    private EntityUid? GetBait(Entity<ADTFishingRodComponent> ent)
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.BaitSlot, out var slot))
            return null;

        return slot.Item;
    }

    public bool IsLavaland(EntityUid uid)
    {
        return HasComp<ADTLavalandMapComponent>(Transform(uid).MapUid);
    }

    public EntProtoId? PickFish(Entity<ADTFishingSpotComponent> spot, EntProtoId? bait, float favoriteChance)
    {
        if (spot.Comp.Junk != null && _random.Prob(spot.Comp.JunkChance))
        {
            var junk = spot.Comp.Junk
                .GetSpawns(_random.GetRandom(), EntityManager, _proto, new EntityTableContext())
                .FirstOrDefault();

            if (junk != default)
                return junk;
        }

        var pool = IsDeep(spot) ? spot.Comp.DeepFish : spot.Comp.ShoreFish;

        if (pool.Count == 0)
            return null;

        if (bait == null || !_random.Prob(favoriteChance))
            return _random.Pick(pool);

        var favorites = new List<EntProtoId>();

        foreach (var id in pool)
        {
            if (!_proto.TryIndex(id, out var proto))
                continue;

            if (!proto.TryGetComponent<ADTFishComponent>(out var fish, _compFactory))
                continue;

            if (fish.FavoriteBait == bait)
                favorites.Add(id);
        }

        if (favorites.Count == 0)
            return _random.Pick(pool);

        return _random.Pick(favorites);
    }

    public bool IsDeep(Entity<ADTFishingSpotComponent> spot)
    {
        if (spot.Comp.Deep is { } cached)
            return cached;

        var deep = CalculateDeep(spot);
        spot.Comp.Deep = deep;
        Dirty(spot);

        return deep;
    }

    private bool CalculateDeep(Entity<ADTFishingSpotComponent> spot)
    {
        var xform = Transform(spot.Owner);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var range = spot.Comp.ShoreRange;

        for (var x = -range; x <= range; x++)
        {
            for (var y = -range; y <= range; y++)
            {
                var indices = center + new Vector2i(x, y);

                if (IsShore(spot.Comp, gridUid, grid, indices))
                    return false;
            }
        }

        return true;
    }

    private bool IsShore(ADTFishingSpotComponent spot, EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        if (spot.ShoreTiles.Count > 0)
        {
            var tileId = _tileDefs[tileRef.Tile.TypeId].ID;

            if (!spot.ShoreTiles.Contains(tileId))
                return false;
        }

        foreach (var uid in _map.GetAnchoredEntities(gridUid, grid, indices))
        {
            if (HasComp<ADTFishingSpotComponent>(uid))
                return false;

            if (_whitelist.IsWhitelistPass(spot.ShoreBlacklist, uid))
                return false;
        }

        return true;
    }
}
