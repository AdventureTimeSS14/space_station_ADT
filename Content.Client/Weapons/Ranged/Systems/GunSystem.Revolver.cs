using Content.Client.ADT.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
// ADT-Tweak Start
//     protected override void InitializeRevolver()
//     {
//         base.InitializeRevolver();
//         SubscribeLocalEvent<RevolverAmmoProviderComponent, AmmoCounterControlEvent>(OnRevolverCounter);
//         SubscribeLocalEvent<RevolverAmmoProviderComponent, UpdateAmmoCounterEvent>(OnRevolverAmmoUpdate);
//         SubscribeLocalEvent<RevolverAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRevolverEntRemove);
//     }
//
// ADT-Tweak End
    [SubscribeLocalEvent]
    private void OnRevolverEntRemove(Entity<RevolverAmmoProviderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != RevolverContainer)
            return;

        // <See ChamberMagazineAmmoProvider>
        if (!IsClientSide(args.Entity))
            return;

        QueueDel(args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnRevolverAmmoUpdate(Entity<RevolverAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
// ADT-Tweak Start
//         if (args.Control is not RevolverStatusControl control) return;
// ADT-Tweak End
        if (args.Control is not RevolverCylinderStatusControl control) return; // Starlight-edit: cylinder view
        control.Update(ent.Comp.CurrentIndex, ent.Comp.Chambers);
    }

    [SubscribeLocalEvent]
    private void OnRevolverCounter(Entity<RevolverAmmoProviderComponent> ent, ref AmmoCounterControlEvent args)
// ADT-Tweak Start
//     {
//         args.Control = new RevolverStatusControl();
//     }
// ADT-Tweak End
        => args.Control = new RevolverCylinderStatusControl(); // Starlight-edit: cylinder view
}
