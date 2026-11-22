using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTRandomChancePrecondition : HTNPrecondition
{
    [Dependency] private IRobustRandom _random = default!;

    [DataField]
    public float Chance = 0.01f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return _random.Prob(Chance);
    }
}
