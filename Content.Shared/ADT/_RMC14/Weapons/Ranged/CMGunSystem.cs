using System.Numerics;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class CMGunSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    private readonly int _blockArcCollisionGroup = (int) (CollisionGroup.HighImpassable | CollisionGroup.Impassable);

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<RMCAmmoEjectComponent, ActivateInWorldEvent>(OnAmmoEjectActivateInWorld);
    }

    private void OnAmmoEjectActivateInWorld(Entity<RMCAmmoEjectComponent> gun, ref ActivateInWorldEvent args)
    {
        if (args.Handled ||
            !_container.TryGetContainer(gun.Owner, gun.Comp.ContainerID, out var container) ||
            container.ContainedEntities.Count <= 0 ||
            !_hands.TryGetActiveHand(args.User, out var hand) ||
            !hand.IsEmpty ||
            !_hands.CanPickupToHand(args.User, container.ContainedEntities[0], hand))
        {
            return;
        }

        var cancelEvent = new RMCTryAmmoEjectEvent(args.User, false);
        RaiseLocalEvent(gun.Owner, ref cancelEvent);

        if (cancelEvent.Cancelled)
            return;

        args.Handled = true;

        var ejectedAmmo = container.ContainedEntities[0];

        // For guns with a BallisticAmmoProviderComponent, if you just remove the ammo from its container, the gun system thinks it's still in the gun and you can still shoot it.
        // So instead I'm having to inflict this shit on our codebase.
        if (TryComp(gun.Owner, out BallisticAmmoProviderComponent? ammoProviderComponent))
        {
            var takeAmmoEvent = new TakeAmmoEvent(1, new List<(EntityUid?, IShootable)>(), Transform(gun.Owner).Coordinates, args.User);
            RaiseLocalEvent(gun.Owner, takeAmmoEvent);

            if (takeAmmoEvent.Ammo.Count <= 0)
                return;

            var ammo = takeAmmoEvent.Ammo[0].Entity;

            if (ammo == null)
                return;

            ejectedAmmo = ammo.Value;
        }

        if (!HasComp<ItemSlotsComponent>(gun.Owner) || !_slots.TryEject(gun.Owner, gun.Comp.ContainerID, args.User, out _, excludeUserAudio: true))
            _audio.PlayPredicted(gun.Comp.EjectSound, gun.Owner, args.User);

        _hands.TryPickup(args.User, ejectedAmmo, hand);
    }
}
