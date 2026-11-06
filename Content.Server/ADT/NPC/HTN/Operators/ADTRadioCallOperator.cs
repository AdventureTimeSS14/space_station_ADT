using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Dataset;
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

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_random.Prob(Chance))
            _entManager.System<ADTNpcRadioSystem>().TryCall(owner, Call, LineSet, Channel, Cooldown, GroupCooldown, GroupRange);

        return HTNOperatorStatus.Finished;
    }
}
