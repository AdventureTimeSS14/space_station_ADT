using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTRadioCallOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    [DataField(required: true)]
    public string Call = string.Empty;

    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> LineSet;

    [DataField]
    public ProtoId<RadioChannelPrototype>? Channel;

    [DataField]
    public float Chance = 1f;

    [DataField]
    public float Cooldown = 30f;

    [DataField]
    public float GroupCooldown = 20f;

    [DataField]
    public float GroupRange = 30f;

    [DataField]
    public ProtoId<ReagentPrototype>? TargetReagent;

    [DataField]
    public FixedPoint2 TargetReagentMin = FixedPoint2.New(1);

    [DataField]
    public string TargetKey = "Target";

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (TargetReagent is { } reagent && !TargetHasReagent(blackboard, reagent))
            return HTNOperatorStatus.Finished;

        if (_random.Prob(Chance))
            _entManager.System<ADTNpcRadioSystem>().TryCall(owner, Call, LineSet, Channel, Cooldown, GroupCooldown, GroupRange);

        return HTNOperatorStatus.Finished;
    }

    private bool TargetHasReagent(NPCBlackboard blackboard, ProtoId<ReagentPrototype> reagent)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            !_entManager.HasComponent<SleepingComponent>(target) ||
            !_entManager.TryGetComponent(target, out BloodstreamComponent? bloodstream))
        {
            return false;
        }

        if (!_entManager.System<SharedSolutionContainerSystem>()
                .TryGetSolution(target, bloodstream.BloodSolutionName, out _, out var solution))
        {
            return false;
        }

        return solution.GetTotalPrototypeQuantity(reagent) >= TargetReagentMin;
    }
}
