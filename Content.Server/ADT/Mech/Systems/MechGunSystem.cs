using Content.Shared.ADT.Mech;
using Content.Shared.ADT.Mech.EntitySystems;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mech.Equipment.EntitySystems;

public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MechGunReloadSystem _reload = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(OnMechGunShot);
        SubscribeLocalEvent<BallisticMechAmmoProviderComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
    }

    private void OnMechGunShot(Entity<MechEquipmentComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<MechComponent>(args.User, out var mech))
            return;

        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is not { } spent || !mech.EquipmentContainer.Contains(spent))
                continue;

            _container.Remove(spent, mech.EquipmentContainer);
            _throwing.TryThrow(spent, _random.NextVector2(), _random.Next(5));
        }
    }

    private void OnUiStateReady(Entity<BallisticMechAmmoProviderComponent> ent, ref MechEquipmentUiStateReadyEvent args)
    {
        _reload.CompleteReload(ent);

        var state = new MechGunUiState
        {
            ReloadTime = ent.Comp.ReloadTime,
            Shots = ent.Comp.Shots,
            Capacity = ent.Comp.Capacity,
            Reloading = _reload.IsReloading(ent.Comp),
            ReloadEndTime = ent.Comp.ReloadEnd,
        };

        args.States[GetNetEntity(ent)] = state;
    }
}
