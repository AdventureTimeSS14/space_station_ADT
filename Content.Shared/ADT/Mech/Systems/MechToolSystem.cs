using Content.Shared.ADT.Mech.Components;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechToolSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, GetUsedEntityEvent>(OnGetUsedEntity);
    }

    private void OnGetUsedEntity(EntityUid uid, MechComponent comp, ref GetUsedEntityEvent args)
    {
        if (args.Handled)
            return;

        if (comp.Energy <= 0)
            return;

        if (comp.CurrentSelectedEquipment is not { } equipment || !HasComp<MechToolComponent>(equipment))
            return;

        args.Used = equipment;
    }
}
