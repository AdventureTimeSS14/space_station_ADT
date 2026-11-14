using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed partial class ShatteredRisenSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShatteredRisenComponent, HandCountChangedEvent>(OnHandsChanged);
        SubscribeLocalEvent<ShatteredRisenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShatteredRisenComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ShatteredRisenComponent> ent, ref ComponentStartup args)
    {
        RemComp<SlowOnDamageComponent>(ent);
        RemComp<BloodstreamComponent>(ent);
    }

    private void OnHandsChanged(Entity<ShatteredRisenComponent> ent, ref HandCountChangedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;
        RefreshHands(ent);
    }

    private void OnMapInit(Entity<ShatteredRisenComponent> ent, ref MapInitEvent args)
    {
        RemComp<SlowOnDamageComponent>(ent);
        RemComp<BloodstreamComponent>(ent);
        RefreshHands(ent);
    }

    private void RefreshHands(Entity<ShatteredRisenComponent> ent)
    {
        if (!TryComp(ent.Owner, out HandsComponent? hands) || hands.Count == 0)
            return;

        var holder = (ent.Owner, hands);
        var hasWeapon1 = false;

        foreach (var held in _hands.EnumerateHeld(holder))
        {
            var proto = MetaData(held).EntityPrototype?.ID;
            if (proto == null)
            {
                DropOrDelete(held);
                continue;
            }

            if (proto == ent.Comp.Weapon1.Id)
                hasWeapon1 = true;
            else if (proto != ent.Comp.Weapon2.Id)
                DropOrDelete(held);

            continue;

            void DropOrDelete(EntityUid item)
            {
                if (!_hands.TryDrop(holder, item, null, false, false))
                    QueueDel(item);
            }
        }

        var coords = Transform(ent).Coordinates;
        foreach (var hand in _hands.EnumerateHands(holder))
        {
            if (_hands.TryGetHeldItem(holder, hand, out _))
                continue;

            var toSpawn = ent.Comp.Weapon1;
            if (!hasWeapon1)
                hasWeapon1 = true;
            else
                toSpawn = ent.Comp.Weapon2;

            var weapon = Spawn(toSpawn, coords);
            if (!_hands.TryForcePickup(holder, weapon, hand, checkActionBlocker: false, animate: false, hands))
                QueueDel(weapon);
        }
    }
}
