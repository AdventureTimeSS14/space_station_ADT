using Content.Shared.Trigger;

namespace Content.Shared.ADT.Implants.ImplantVision;

public sealed class ImplantVisionOnTriggerSystem : XOnTriggerSystem<ImplantVisionOnTriggerComponent>
{
    [Dependency] private readonly ImplantVisionSystem _vision = default!;

    protected override void OnTrigger(Entity<ImplantVisionOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _vision.Add(target, ent.Comp.Vision, ent.Comp.Duration);
        args.Handled = true;
    }
}
