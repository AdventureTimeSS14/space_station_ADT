using Content.Shared.ADT.Fishing;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Fishing;

public sealed class ADTCharredKrillSystem : EntitySystem
{
    [Dependency] private readonly ADTFishingSystem _fishing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCharredKrillComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTCharredKrillComponent, ADTCharredKrillDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ADTCharredKrillComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<ADTCharredKrillComponent, EntityTerminatingEvent>(OnTerminating);
    }

    private void OnAfterInteract(Entity<ADTCharredKrillComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || ent.Comp.InLava || args.Target is not { } target)
            return;

        if (!HasComp<ADTFishingSpotComponent>(target))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.PlaceTime, new ADTCharredKrillDoAfterEvent(), ent.Owner, target, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("adt-charred-krill-place"), ent.Owner, args.User);
    }

    private void OnDoAfter(Entity<ADTCharredKrillComponent> ent, ref ADTCharredKrillDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<ADTFishingSpotComponent>(target, out var spot))
            return;

        args.Handled = true;
        Drown(ent, (target, spot));
    }

    private void OnLand(Entity<ADTCharredKrillComponent> ent, ref LandEvent args)
    {
        if (ent.Comp.InLava)
            return;

        if (FindSpot(ent.Owner) is not { } spot)
            return;

        Drown(ent, spot);
    }

    private void OnTerminating(Entity<ADTCharredKrillComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.InLava)
            return;

        if (FindSpot(ent.Owner) is not { } spot)
            return;

        if (!spot.Comp.CanBeFished || (spot.Comp.LavalandOnly && !_fishing.IsLavaland(spot.Owner)))
        {
            _popup.PopupCoordinates(Loc.GetString("adt-charred-krill-nothing"), Transform(ent.Owner).Coordinates);
            return;
        }

        _popup.PopupCoordinates(Loc.GetString("adt-charred-krill-fish"), Transform(ent.Owner).Coordinates, PopupType.MediumCaution);

        for (var i = 0; i < spot.Comp.KrillFish; i++)
        {
            if (_fishing.PickFish(spot, null, 0f) is not { } fish)
                continue;

            Spawn(fish, Transform(spot.Owner).Coordinates);
        }
    }

    private void Drown(Entity<ADTCharredKrillComponent> ent, Entity<ADTFishingSpotComponent> spot)
    {
        ent.Comp.InLava = true;

        _transform.SetCoordinates(ent.Owner, Transform(spot.Owner).Coordinates);
        _transform.AnchorEntity(ent.Owner, Transform(ent.Owner));

        RemComp<ItemComponent>(ent.Owner);
        RemComp<PullableComponent>(ent.Owner);

        var despawn = EnsureComp<TimedDespawnComponent>(ent.Owner);
        despawn.Lifetime = (float)spot.Comp.KrillDelay.TotalSeconds;

        _popup.PopupCoordinates(Loc.GetString("adt-charred-krill-sink"), Transform(ent.Owner).Coordinates);
    }

    private Entity<ADTFishingSpotComponent>? FindSpot(EntityUid uid)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return null;

        var indices = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var anchored = _map.GetAnchoredEntities(gridUid, grid, indices);

        foreach (var ent in anchored)
        {
            if (TryComp<ADTFishingSpotComponent>(ent, out var spot))
                return (ent, spot);
        }

        return null;
    }
}
