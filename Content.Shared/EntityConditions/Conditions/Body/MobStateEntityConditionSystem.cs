using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if this entity's current mob state matches the condition's specified mob state.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class MobStateEntityConditionSystem : EntityConditionSystem<MobStateComponent, MobStateCondition>
{
    protected override void Condition(Entity<MobStateComponent> entity, ref EntityConditionEvent<MobStateCondition> args)
    {
        // ADT-Tweak-Start
        if (args.Condition.Mobstates is { Length: > 0 } states)
        {
            args.Result = Array.IndexOf(states, entity.Comp.CurrentState) >= 0;
            return;
        }
        // ADT-Tweak-End

        if (entity.Comp.CurrentState == args.Condition.Mobstate)
            args.Result = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MobStateCondition : EntityConditionBase<MobStateCondition>
{
    [DataField]
    public MobState Mobstate = MobState.Alive;

    /// <summary>
    /// ADT-Tweak: если задан, условие проходит при любом из перечисленных состояний.
    /// </summary>
    [DataField]
    public MobState[]? Mobstates;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-mob-state-condition",
            ("state", Mobstates is { Length: > 0 } states ? string.Join(", ", states) : Mobstate.ToString())); // ADT-Tweak
}
