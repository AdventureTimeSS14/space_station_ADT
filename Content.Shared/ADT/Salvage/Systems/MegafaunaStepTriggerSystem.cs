using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;

namespace Content.Shared.ADT.Salvage.Systems;

public sealed class MegafaunaStepTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    private void OnStepTriggerAttempt(Entity<StepTriggerComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (!HasComp<MegafaunaComponent>(args.Tripper)
            && !HasComp<ADTLavaBoatComponent>(args.Tripper)
            && !HasComp<ADTLavaBoatRiderComponent>(args.Tripper))
            return;

        args.Cancelled = true;
    }
}
