using Content.Shared.ADT.Mech.Equipment.Components;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeMechGun()
    {
        base.Initialize();
        SubscribeLocalEvent<MechGunComponent, ShotAttemptedEvent>(OnShotAttempt);

        // SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, ComponentGetState>(OnGetState);
        // SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

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
    }

    // private void OnGetState(EntityUid uid, ProjectileMechAmmoProviderComponent component, ref ComponentGetState args)
    // {
    //     args.State = new MechAmmoProviderComponentState()
    //     {
    //         Shots = component.Shots,
    //         MaxShots = component.Capacity,
    //     };
    // }

    // private void OnHandleState(EntityUid uid, ProjectileMechAmmoProviderComponent component, ref ComponentHandleState args)
    // {
    //     if (args.Current is not MechAmmoProviderComponentState state)
    //         return;

    //     component.Shots = state.Shots;
    //     component.Capacity = state.MaxShots;
    // }


    private void OnMechAmmoCount(EntityUid uid, ProjectileMechAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = component.Shots;
        args.Capacity = component.Capacity;
    }

    private void OnTakeAmmo(EntityUid uid, ProjectileMechAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, component.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        if (component.Reloading)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetShootable(component, args.Coordinates));
            component.Shots--;
        }

        if (_netManager.IsServer)
            Dirty(uid, component);
    }

    private (EntityUid? Entity, IShootable) GetShootable(ProjectileMechAmmoProviderComponent component, EntityCoordinates coordinates)
    {
        switch (component)
        {
            case ProjectileMechAmmoProviderComponent proj:
                var ent = Spawn(proj.Prototype, coordinates);
                return (ent, EnsureShootable(ent));
            // case HitscanBatteryAmmoProviderComponent hitscan:
            //     return (null, ProtoManager.Index<HitscanPrototype>(hitscan.Prototype));
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
