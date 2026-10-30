using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class SharpMedalSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<EntityUid> _returning = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticBladeComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<HereticBladeComponent, LandEvent>(OnThrowLand);
        SubscribeLocalEvent<HereticBladeComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnThrowHit(Entity<HereticBladeComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp(args.Thrown, out ThrownItemComponent? thrown) || thrown.Thrower is not { } thrower)
            return;

        ScheduleReturn(thrower, args.Thrown);
    }

    private void OnThrowLand(Entity<HereticBladeComponent> ent, ref LandEvent args)
    {
        if (args.User is not { } thrower)
            return;

        ScheduleReturn(thrower, ent);
    }

    private void OnStopThrow(Entity<HereticBladeComponent> ent, ref StopThrowEvent args)
    {
        if (args.User is not { } thrower)
            return;

        ScheduleReturn(thrower, ent);
    }

    private void ScheduleReturn(EntityUid thrower, EntityUid blade)
    {
        if (_net.IsClient)
            return;

        if (!HasMedal(thrower))
            return;

        if (_hands.IsHolding(thrower, blade, out _))
            return;

        if (!_returning.Add(blade))
            return;

        Timer.Spawn(350, () =>
        {
            try
            {
                Return(thrower, blade);
            }
            finally
            {
                _returning.Remove(blade);
            }
        });
    }

    private void Return(EntityUid thrower, EntityUid blade)
    {
        if (TerminatingOrDeleted(blade) || TerminatingOrDeleted(thrower))
            return;

        if (!HasMedal(thrower))
            return;

        if (_hands.IsHolding(thrower, blade, out _))
            return;

        if (TryComp<PhysicsComponent>(blade, out var physics))
        {
            _physics.SetLinearVelocity(blade, System.Numerics.Vector2.Zero, body: physics);
            _physics.SetAngularVelocity(blade, 0f, body: physics);
        }

        RemComp<ThrownItemComponent>(blade);

        if (!TryComp(thrower, out HandsComponent? hands))
            return;

        _hands.PickupOrDrop(thrower, blade, checkActionBlocker: false, animate: true, dropNear: true, handsComp: hands);
    }

    private bool HasMedal(EntityUid user)
    {
        if (_inventory.TryGetContainerSlotEnumerator(user, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out _))
            {
                if (HasComp<SharpMedalComponent>(item))
                    return true;
            }
        }

        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands)))
            {
                if (HasComp<SharpMedalComponent>(held))
                    return true;
            }
        }

        return false;
    }
}
