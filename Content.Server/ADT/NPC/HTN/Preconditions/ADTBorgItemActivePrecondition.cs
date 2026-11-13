using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTBorgItemActivePrecondition : HTNPrecondition
{
    private ADTNpcGearSystem _gear = default!;

    [DataField(required: true)]
    public List<EntProtoId> Items = new();

    [DataField]
    public bool Invert;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _gear.IsBorgItemActive(owner, Items) ^ Invert;
    }
}
