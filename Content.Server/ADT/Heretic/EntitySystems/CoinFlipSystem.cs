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
        var lookup = _lookup.GetEntitiesInRange(ent, 5f);

        if (side.Effect == "toggle_bolts")
        {
            foreach (var target in lookup)
            {
                if (!TryComp<DoorBoltComponent>(target, out var bolts))
                    continue;

                _door.SetBoltsDown((target, bolts), !bolts.BoltsDown);
            }

            return;
        }

        foreach (var target in lookup)
        {
            if (!HasComp<DoorComponent>(target))
                continue;

            _door.TryToggleDoor(target, user: user);
        }
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
