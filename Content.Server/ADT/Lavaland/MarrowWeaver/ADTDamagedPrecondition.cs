using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Damage.Components;

namespace Content.Server.ADT.Lavaland.MarrowWeaver;

public sealed partial class ADTDamagedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _entManager.TryGetComponent<DamageableComponent>(owner, out var damageable) && damageable.TotalDamage > 0;
    }
}
