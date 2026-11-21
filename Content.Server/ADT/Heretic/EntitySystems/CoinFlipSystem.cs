//

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Server.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class CoinFlipSystem : SharedCoinFlipSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlipComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<CoinFlipComponent>();
        while (query.MoveNext(out var uid, out var coin))
        {
            if (!coin.IsFlipping)
                continue;

            if (now < coin.FlipEndTime)
                continue;

            coin.IsFlipping = false;
            coin.CurrentSide = _random.Pick(coin.Sides);

            _popup.PopupEntity(Loc.GetString("coin-flip-popup-message",
                    ("coin", uid),
                    ("side", Loc.GetString(coin.CurrentSide.Name))),
                uid);
            Appearance.SetData(uid, CoinFlipVisuals.SpriteState, coin.CurrentSide.SpriteState);

            if (coin.User is { } user)
                ApplySideEffect((uid, coin), coin.CurrentSide, user);

            coin.User = null;
            Dirty(uid, coin);
        }
    }

    private void ApplySideEffect(Entity<CoinFlipComponent> ent, CoinSide side, EntityUid user)
    {
        var target = FindNearestDoor(ent.Owner, 2f);
        if (target == null)
            return;
        if (side.Effect == "toggle_bolts")
        {
            if (!TryComp<DoorBoltComponent>(target.Value, out var bolts))
                return;
            _door.SetBoltsDown((target.Value, bolts), !bolts.BoltsDown);
            return;
        }
        if (side.Effect != "toggle_doors")
            return;
        if (!TryComp<DoorComponent>(target.Value, out var door))
            return;
        if (door.State is DoorState.Closed or DoorState.Denying)
            _door.StartOpening(target.Value, door);
    }

    private EntityUid? FindNearestDoor(EntityUid coin, float range)
    {
        var coinCoords = Transform(coin).Coordinates;
        EntityUid? nearest = null;
        var nearestDist = float.MaxValue;
        foreach (var target in _lookup.GetEntitiesInRange(coin, range))
        {
            if (!HasComp<DoorComponent>(target))
                continue;
            if (!coinCoords.TryDistance(EntityManager, Transform(target).Coordinates, out var dist))
                continue;
            if (dist >= nearestDist)
                continue;
            nearestDist = dist;
            nearest = target;
        }
        return nearest;
    }

    private void OnInit(Entity<CoinFlipComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Sides.Count == 0)
        {
            Log.Error($"{ToPrettyString(ent)} has 0 coin sides");
            QueueDel(ent);
            return;
        }

        ent.Comp.IsFlipping = false;
        ent.Comp.CurrentSide ??= _random.Pick(ent.Comp.Sides);
        Appearance.SetData(ent, CoinFlipVisuals.SpriteState, ent.Comp.CurrentSide.SpriteState);
        Dirty(ent);
    }
}
