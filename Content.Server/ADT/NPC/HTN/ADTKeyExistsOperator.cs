using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ADTKeyExistsOperator : HTNOperator
{
    [DataField(required: true)]
    public string Key = string.Empty;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.ContainsKey(Key))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
