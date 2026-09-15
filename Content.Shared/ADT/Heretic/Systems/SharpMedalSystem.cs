//

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class SharpMedalSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticBladeComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnThrowHit(Entity<HereticBladeComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp(args.Thrown, out ThrownItemComponent? thrown) || thrown.Thrower is not { } thrower)
            return;

        if (!HasMedal(thrower))
            return;

        var blade = args.Thrown;
        Timer.Spawn(600, () =>
        {
            if (TerminatingOrDeleted(blade) || TerminatingOrDeleted(thrower))
                return;

            if (!TryComp(thrower, out HandsComponent? hands))
                return;

            if (_hands.TryGetEmptyHand((thrower, hands), out var hand))
                _hands.TryPickup(thrower, blade, hand, handsComp: hands);
            else
                _transform.SetCoordinates(blade, Transform(thrower).Coordinates);
        });
    }

    private bool HasMedal(EntityUid user)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator))
            return false;

        while (enumerator.NextItem(out var item, out _))
        {
            if (HasComp<SharpMedalComponent>(item))
                return true;
        }

        return false;
    }
}
