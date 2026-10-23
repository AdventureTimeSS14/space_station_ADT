using Content.Shared.Trigger;

namespace Content.Shared.ADT.Implants.ImplantBoost;

public sealed class ImplantBoostOnTriggerSystem : XOnTriggerSystem<ImplantBoostOnTriggerComponent>
{
    [Dependency] private readonly ImplantBoostSystem _boost = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantBoostOnTriggerComponent, AttemptTriggerEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<ImplantBoostOnTriggerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target is { } uid && HasComp<ImplantBoostComponent>(uid))
            args.Cancelled = true;
    }

    protected override void OnTrigger(Entity<ImplantBoostOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _boost.Boost(target, ent.Comp.Settings, ent.Comp.Duration);
        args.Handled = true;
    }
}
