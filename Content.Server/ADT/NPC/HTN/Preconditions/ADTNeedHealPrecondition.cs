using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTNeedHealPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float MinDamage = 0.1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var hurt = ADTNpcHealHelper.IsBleeding(_entManager, owner)
            || ADTNpcHealHelper.DamageFraction(_entManager, owner) >= MinDamage;

        return hurt && ADTNpcHealHelper.FindHealItem(
            _entManager, owner, ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager)) != null;
    }
}
