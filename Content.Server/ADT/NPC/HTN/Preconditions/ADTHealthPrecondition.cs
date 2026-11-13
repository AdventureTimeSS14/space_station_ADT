using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTHealthPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Below = 0.4f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var health = 1f - _entManager.System<ADTNpcGearSystem>().DamageFraction(owner);

        return (health < Below) ^ Invert;
    }
}
