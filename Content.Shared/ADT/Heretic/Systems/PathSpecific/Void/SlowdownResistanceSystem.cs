//

using Content.Shared.Heretic.Components.PathSpecific.Void;
using Content.Shared.Movement.Systems;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Void;

public sealed partial class SlowdownResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlowdownResistanceComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<SlowdownResistanceComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.WalkBonus, ent.Comp.SprintBonus);
    }
}
