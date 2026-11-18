using Content.Server.ADT.Mech.Equipment.Components;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Mech;

namespace Content.Server.ADT.Mech.Equipment.EntitySystems;

public sealed class MechArmorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechArmorComponent, MechEquipmentInsertedEvent>(OnEquipmentInstalled);
        SubscribeLocalEvent<MechArmorComponent, MechEquipmentRemovedEvent>(OnEquipmentRemoved);
    }

    private void OnEquipmentInstalled(Entity<MechArmorComponent> ent, ref MechEquipmentInsertedEvent args)
    {
        if (!TryComp<ADTMechComponent>(args.Mech, out var mech))
            return;

        ent.Comp.OriginalModifiers = mech.Modifiers;
        mech.Modifiers = ent.Comp.Modifiers;
    }

    private void OnEquipmentRemoved(Entity<MechArmorComponent> ent, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<ADTMechComponent>(args.Mech, out var mech))
            return;

        mech.Modifiers = ent.Comp.OriginalModifiers;
        ent.Comp.OriginalModifiers = null;
    }
}
