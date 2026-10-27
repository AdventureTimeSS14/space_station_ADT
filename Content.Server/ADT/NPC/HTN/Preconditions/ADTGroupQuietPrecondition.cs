using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.NPC.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTGroupQuietPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<Entity<ADTNpcCombatMemoryComponent>> _found = new();

    [DataField]
    public float Seconds = 10f;

    [DataField]
    public float Range = 15f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var transform = _entManager.System<SharedTransformSystem>();
        var faction = _entManager.System<NpcFactionSystem>();
        var now = _timing.CurTime;

        _found.Clear();
        _entManager.System<EntityLookupSystem>().GetEntitiesInRange(transform.GetMapCoordinates(owner), Range, _found);

        foreach (var found in _found)
        {
            var other = found.Owner;

            if (other != owner && !faction.IsEntityFriendly(owner, other))
                continue;

            if (found.Comp.Spoke && (now - found.Comp.LastSpoke).TotalSeconds < Seconds)
                return false;
        }

        return true;
    }
}
