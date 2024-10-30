using Content.Shared.ADT.Mech.Equipment.Components;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedMechSystem _mech = default!;

    protected virtual void InitializeMechGun()
    {
        base.Initialize();
        SubscribeLocalEvent<MechGunComponent, ShotAttemptedEvent>(OnShotAttempt);

        SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

    }

    private void OnShotAttempt(EntityUid uid, MechGunComponent comp, ref ShotAttemptedEvent args)
    {
        if (!TryComp<MechComponent>(args.User, out var mech))
        {
            args.Cancel();
            return;
        }

        if (mech.Energy.Float() <= 0f)
            args.Cancel();

        if (TryComp<ProjectileMechAmmoProviderComponent>(uid, out var projMech) && projMech.Shots <= 0)
            args.Cancel();
    }

    private void OnMechAmmoCount(EntityUid uid, MechAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        switch (component)
        {
            case ProjectileMechAmmoProviderComponent projectile:
                args.Count = projectile.Shots;
                args.Capacity = projectile.Capacity;
                break;
            case HitscanMechAmmoProviderComponent hitscan:
                args.Count = 5;
                args.Capacity = 5;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnTakeAmmo(EntityUid uid, MechAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var equipmentComp = Comp<MechEquipmentComponent>(uid);

        switch (component)
        {
            case ProjectileMechAmmoProviderComponent projectile:
                var shots = Math.Min(args.Shots, projectile.Shots);

                // Don't dirty if it's an empty fire.
                if (shots == 0)
                    return;

                if (projectile.Reloading)
                    return;

                for (var i = 0; i < shots; i++)
                {
                    args.Ammo.Add(GetShootable(projectile, args.Coordinates));
                    projectile.Shots--;
                }
                break;
            case HitscanMechAmmoProviderComponent hitscan:
                if (args.Shots == 0)
                    return;

                for (var i = 0; i < args.Shots; i++)
                {
                    args.Ammo.Add(GetShootable(hitscan, args.Coordinates));
                    if (!equipmentComp.EquipmentOwner.HasValue)
                        break;
                    if (!_mech.TryChangeEnergy(equipmentComp.EquipmentOwner.Value, -hitscan.ShotCost))
                        break;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_netManager.IsServer)
            Dirty(uid, component);
    }

    private (EntityUid? Entity, IShootable) GetShootable(MechAmmoProviderComponent component, EntityCoordinates coordinates)
    {
        switch (component)
        {
            case ProjectileMechAmmoProviderComponent proj:
                var ent = Spawn(proj.Prototype, coordinates);
                return (ent, EnsureShootable(ent));
            case HitscanMechAmmoProviderComponent hitscan:
                return (null, ProtoManager.Index(hitscan.Proto));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [Serializable, NetSerializable]
    private sealed class MechAmmoProviderComponentState : ComponentState
    {
        public int Shots;
        public int MaxShots;
    }

}
