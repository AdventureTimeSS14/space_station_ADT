using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTNeedHealPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;

    [DataField]
    public float MinDamage = 0.1f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_gear.IsBleeding(owner) && _gear.DamageFraction(owner) < MinDamage)
            return false;

        var ignored = ADTNpcGearSystem.GetIgnored(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, _entManager);
        return _gear.FindHealItem(owner, ignored, out _) != null;
    }
}
