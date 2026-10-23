using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTRadioCallPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public List<string> Types = new();

    [DataField]
    public float MaxAge = 30f;

    [DataField]
    public float MinDistance = 6f;

    [DataField]
    public float MaxDistance = 40f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _entManager.System<ADTNpcRadioSystem>().FindCall(owner, Types, MaxAge, MinDistance, MaxDistance) != null;
    }
}
