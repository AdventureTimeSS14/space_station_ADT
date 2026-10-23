using Content.Shared.Trigger;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Implants.ImplantActivationVision;

public sealed class ImplantVisionOnTriggerSystem : XOnTriggerSystem<ImplantVisionOnTriggerComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void OnTrigger(Entity<ImplantVisionOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var vision = EnsureComp<ImplantActivationVisionComponent>(target);
        vision.StartTime = _timing.CurTime;
        vision.EndTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(target, vision);

        args.Handled = true;
    }
}
