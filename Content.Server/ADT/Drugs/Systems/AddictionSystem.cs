using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Movement.Components;
using Content.Server.Movement.Components;

public sealed class AddictionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ReagentEffectComponent, ReagentEffectEvent>(OnReagentEffect);
    }

    private void OnReagentEffect(EntityUid uid, ReagentEffectComponent component, ReagentEffectEvent args)
    {
        if (args.Scale <= 0f || args.SolutionEntity is not { Valid: true } target)
            return;

        if (!TryComp<AddictionTriggerComponent>(target, out var addictionTrigger))
            return;

        if (!addictionTrigger.ReagentAddictions.TryGetValue(args.Reagent.ID, out var addictionType))
            return;

        EnsureComp<AddictionComponent>(target);

        switch (addictionType)
        {
            case AddictionType.Tea:
                EnsureComp<TeaAddictionComponent>(target);
                break;
            case AddictionType.Drug:
                EnsureComp<DrugAddictionComponent>(target);
                break;
            case AddictionType.Tobacco:
                EnsureComp<TobaccoAddictionComponent>(target);
                break;
            case AddictionType.Coffee:
                EnsureComp<CoffeeAddictionComponent>(target);
                break;
        }
    }
}