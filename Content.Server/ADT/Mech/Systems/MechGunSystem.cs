using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Content.Shared.ADT.Mech.Equipment.Components;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Mech;
using Content.Shared.ADT.Mech;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedGunSystem _guns = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(MechGunShot);

        SubscribeLocalEvent<ProjectileMechAmmoProviderComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeNetworkEvent<MechGunReloadMessage>(OnReload);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProjectileMechAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var gun))
        {
            if (gun.Reloading && gun.ReloadEnd <= _timing.CurTime)
            {
                gun.Reloading = false;
                gun.Shots = gun.Capacity;
                Dirty(uid, gun);
            }
        }
    }
    private void MechGunShot(EntityUid uid, MechEquipmentComponent component, ref GunShotEvent args)
    {
        if (!component.EquipmentOwner.HasValue)
        {
            if (TryComp<MechComponent>(args.User, out var pilot) && pilot.PilotSlot.ContainedEntity != null)
                _mech.TryEject(args.User, pilot);
            _stun.TryParalyze(args.User, TimeSpan.FromSeconds(10), true);
            _throwing.TryThrow(args.User, _random.NextVector2(), _random.Next(50));
            return;
        }
        if (!TryComp<MechComponent>(component.EquipmentOwner.Value, out var mech))
        {
            return;
        }
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            ChargeGunBattery(uid, battery);
            return;
        }
        if (HasComp<ProjectileMechAmmoProviderComponent>(uid))
            _mech.UpdateUserInterface(component.EquipmentOwner.Value);


        // In most guns the ammo itself isn't shot but turned into cassings
        // and a new projectile is spawned instead, meaning that args.Ammo
        // is most likely inside the equipment container (for some odd reason)

        // I'm not even sure why this is needed since GunSystem.Shoot() has a
        // container check before ejecting, but yet it still puts the spent ammo inside the mech
        foreach (var (ent, _) in args.Ammo)
        {
            if (ent.HasValue && mech.EquipmentContainer.Contains(ent.Value))
            {
                _container.Remove(ent.Value, mech.EquipmentContainer);
                _throwing.TryThrow(ent.Value, _random.NextVector2(), _random.Next(5));
            }
        }
    }

    private void ChargeGunBattery(EntityUid uid, BatteryComponent component)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var mechEquipment) || !mechEquipment.EquipmentOwner.HasValue)
            return;

        if (!TryComp<MechComponent>(mechEquipment.EquipmentOwner.Value, out var mech))
            return;

        var maxCharge = component.MaxCharge;
        var currentCharge = component.CurrentCharge;

        var chargeDelta = maxCharge - currentCharge;

        if (chargeDelta <= 0 || mech.Energy - chargeDelta < 0)
            return;
        if (TryComp<MechGunComponent>(uid, out var mechGun))
            chargeDelta *= mechGun.BatteryUsageMultiplier;

        if (!_mech.TryChangeEnergy(mechEquipment.EquipmentOwner.Value, -chargeDelta, mech))
            return;

        _battery.SetCharge(uid, component.MaxCharge, component);
    }

    private void OnUiStateReady(EntityUid uid, ProjectileMechAmmoProviderComponent component, MechEquipmentUiStateReadyEvent args)
    {
        var state = new MechGunUiState
        {
            ReloadTime = component.ReloadTime,
            Shots = component.Shots,
            Capacity = component.Capacity,
        };
        args.States.Add(GetNetEntity(uid), state);
    }

    private void OnReload(MechGunReloadMessage args)
    {
        var uid = GetEntity(args.Equipment);
        var comp = EnsureComp<ProjectileMechAmmoProviderComponent>(uid);

        comp.Reloading = true;
        comp.ReloadEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.ReloadTime);
        Dirty(uid, comp);
    }
}
