using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTBorgItemSolutionPrecondition : HTNPrecondition
{
    private ADTNpcGearSystem _gear = default!;
    private SharedSolutionContainerSystem _solutions = default!;

    [DataField(required: true)]
    public List<EntProtoId> Items = new();

    [DataField(required: true)]
    public string Solution = string.Empty;

    [DataField]
    public FixedPoint2 MinVolume = FixedPoint2.New(5);

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
        _solutions = sysManager.GetEntitySystem<SharedSolutionContainerSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_gear.FindBorgItem(owner, Items) is not { } item)
            return _gear.FindBorgModule(owner, Items) != null;

        if (!_solutions.TryGetSolution(item, Solution, out _, out var solution))
            return false;

        return solution.Volume >= MinVolume;
    }
}
