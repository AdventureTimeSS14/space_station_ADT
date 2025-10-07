using System.Numerics;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;
public sealed class CMGunSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private bool _isRevolverActionInProgress = false;
    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        SubscribeLocalEvent<ShootAtFixedPointComponent, AmmoShotEvent>(OnShootAtFixedPointShot);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, UniqueActionEvent>(OnRevolverUniqueAction);
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

    private void OnRevolverUniqueAction(Entity<RevolverAmmoProviderComponent> gun, ref UniqueActionEvent args)
    {
        if (args.Handled || _isRevolverActionInProgress)
            return;

        _isRevolverActionInProgress = true;

        try
        {
            int randomCount = _random.Next(1, gun.Comp.Capacity + 1);

            gun.Comp.CurrentIndex = (gun.Comp.CurrentIndex + randomCount) % gun.Comp.Capacity;

            _audio.PlayPredicted(gun.Comp.SoundSpin, gun.Owner, args.UserUid);
            var popup = Loc.GetString("rmc-revolver-spin", ("gun", args.UserUid));
            _popup.PopupClient(popup, args.UserUid, args.UserUid, PopupType.SmallCaution);

            Dirty(gun);
        }
        finally
        {
            _isRevolverActionInProgress = false;
        }
    }

    /// <summary>
    /// Shoot at a targeted point's coordinates. The projectile will stop at that location instead of continuing on until it hits something.
    /// There is also an option to arc the projectile with ShootArcProj or ArcProj = true, making it ignore most collision.
    /// </summary>
    /// <remarks>
    /// For some reason, the engine seem to cause MaxFixedRange's conversion to actual projectile max ranges of around +1 tile.
    /// As a result, conversions should be 1 less than max_range when porting, and the minimum range for this feature is around 2 tiles.
    /// This could be manually tweaked try and fix it, but the math seems like it should be fine and it's predictable enough to be worked around for now.
    /// </remarks>
    private void OnShootAtFixedPointShot(Entity<ShootAtFixedPointComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp(ent, out GunComponent? gun) ||
            gun.ShootCoordinates is not { } target)
        {
            return;
        }

        // Find start and end coordinates for vector.
        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.ToMapCoordinates(target);
        // Must be same map.
        if (from.MapId != to.MapId)
            return;

        // Calculate vector, cancel if it ends up at 0.
        var direction = to.Position - from.Position;
        if (direction == Vector2.Zero)
            return;

        // Check for a max range from the ShootAtFixedPointComponent. If defined, take the minimum between that and the calculated distance.
        var distance = ent.Comp.MaxFixedRange != null ? Math.Min(ent.Comp.MaxFixedRange.Value, direction.Length()) : direction.Length();
        // Get current time and normalize the vector for physics math.
        var time = _timing.CurTime;
        var normalized = direction.Normalized();

        // Send each FiredProjectile with a PhysicsComponent off with the same Vector. Max
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_physicsQuery.TryComp(projectile, out var physics))
                continue;

            // Calculate needed impulse to get to target, remove all velocity from projectile, then apply.
            var impulse = normalized * gun.ProjectileSpeedModified * physics.Mass;
            _physics.SetLinearVelocity(projectile, Vector2.Zero, body: physics);
            _physics.ApplyLinearImpulse(projectile, impulse, body: physics);
            _physics.SetBodyStatus(projectile, physics, BodyStatus.InAir);

            // Apply the ProjectileFixedDistanceComponent onto each fired projectile, which both holds the FlyEndTime to be continually checked
            // and will trigger the OnEventToStopProjectile function once the PFD Component is deleted at that time. See Update()
            var comp = EnsureComp<ProjectileFixedDistanceComponent>(projectile);

            // Transfer arcing to the projectile.
            if (Comp<ShootAtFixedPointComponent>(ent).ShootArcProj)
                comp.ArcProj = true;

            // Take the lowest nonzero MaxFixedRange between projectile and gun for the capped vector length.
            if (TryComp(projectile, out ProjectileComponent? normalProjectile) && normalProjectile.MaxFixedRange > 0)
            {
                distance = distance > 0 ? Math.Min(normalProjectile.MaxFixedRange.Value, distance) : normalProjectile.MaxFixedRange.Value;
            }
            // Calculate travel time and equivalent distance based either on click location or calculated max range, whichever is shorter.
            comp.FlyEndTime = time + TimeSpan.FromSeconds(distance / gun.ProjectileSpeedModified);
        }
    }
}
