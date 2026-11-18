using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Mech.EntitySystems;
using Content.Shared.ADT.Mech.Equipment.Components;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private SharedMechSystem _mech = default!;
    [Dependency] private SharedADTMechSystem _adtMech = default!;
    [Dependency] private MechGunReloadSystem _mechReload = default!;

    protected virtual void InitializeMechGun()
    {
        SubscribeLocalEvent<MechGunComponent, ShotAttemptedEvent>(OnMechShotAttempt);

        SubscribeLocalEvent<BallisticMechAmmoProviderComponent, TakeAmmoEvent>(OnBallisticMechTakeAmmo);
        SubscribeLocalEvent<BallisticMechAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticMechAmmoCount);

        SubscribeLocalEvent<BatteryMechAmmoProviderComponent, TakeAmmoEvent>(OnBatteryMechTakeAmmo);
        SubscribeLocalEvent<BatteryMechAmmoProviderComponent, GetAmmoCountEvent>(OnBatteryMechAmmoCount);

        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, TakeAmmoEvent>(OnHitscanMechTakeAmmo);
        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, GetAmmoCountEvent>(OnHitscanMechAmmoCount);
    }

    private void OnMechShotAttempt(Entity<MechGunComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!TryComp<MechComponent>(args.User, out var mech) || _adtMech.GetEquipmentMech(ent) != args.User)
        {
            args.Cancel();
            return;
        }

        if (mech.Energy <= 0 || !_adtMech.CanSelect(ent))
        {
            args.Cancel();
            return;
        }

        if (!TryComp<BallisticMechAmmoProviderComponent>(ent, out var ammo))
            return;

        _mechReload.CompleteReload((ent, ammo));
        if (ammo.Shots <= 0 || _mechReload.IsReloading(ammo))
            args.Cancel();
    }

    private void OnBallisticMechAmmoCount(Entity<BallisticMechAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        _mechReload.CompleteReload(ent);
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.Capacity;
    }

    private void OnBatteryMechAmmoCount(Entity<BatteryMechAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        GetEnergyShots(ent, ent.Comp.ShotCost, ref args);
    }

    private void OnHitscanMechAmmoCount(Entity<HitscanMechAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        GetEnergyShots(ent, ent.Comp.ShotCost, ref args);
    }

    private void GetEnergyShots(EntityUid gun, float cost, ref GetAmmoCountEvent args)
    {
        if (_adtMech.GetEquipmentMech(gun) is not { } mechUid ||
            !TryComp<MechComponent>(mechUid, out var mech) ||
            cost <= 0f)
        {
            args.Count = 0;
            args.Capacity = 0;
            return;
        }

        args.Count = (int) (mech.Energy.Float() / cost);
        args.Capacity = (int) (mech.MaxEnergy.Float() / cost);
    }

    private void OnBallisticMechTakeAmmo(Entity<BallisticMechAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        _mechReload.CompleteReload(ent);

        if (_mechReload.IsReloading(ent.Comp))
            return;

        var shots = Math.Min(args.Shots, ent.Comp.Shots);
        if (shots <= 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            var bullet = Spawn(ent.Comp.Prototype, args.Coordinates);
            args.Ammo.Add((bullet, EnsureShootable(bullet)));
        }

        ent.Comp.Shots -= shots;
        Dirty(ent);
    }

    private void OnBatteryMechTakeAmmo(Entity<BatteryMechAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        if (_adtMech.GetEquipmentMech(ent) is not { } mech)
            return;

        for (var i = 0; i < args.Shots; i++)
        {
            if (!_mech.TryChangeEnergy(mech, -ent.Comp.ShotCost))
                break;

            var bullet = Spawn(ent.Comp.Prototype, args.Coordinates);
            args.Ammo.Add((bullet, EnsureShootable(bullet)));
        }
    }

    private void OnHitscanMechTakeAmmo(Entity<HitscanMechAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        if (_adtMech.GetEquipmentMech(ent) is not { } mech)
            return;

        var weakened = HasComp<MechSecondPilotControlComponent>(mech) && ent.Comp.WeakenedProto != null;
        var proto = weakened ? ent.Comp.WeakenedProto!.Value : ent.Comp.Proto;
        var cost = weakened ? ent.Comp.ShotCost * ent.Comp.WeakenedCostModifier : ent.Comp.ShotCost;

        for (var i = 0; i < args.Shots; i++)
        {
            if (!_mech.TryChangeEnergy(mech, -cost))
                break;

            var shot = Spawn(proto, args.Coordinates);
            args.Ammo.Add((shot, EnsureShootable(shot)));
        }
    }
}
