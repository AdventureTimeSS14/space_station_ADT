using Content.Server.NPC.HTN;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server.ADT.NPC.Helpers;

public static class ADTNpcGroupHelper
{
    public static EntityUid FindLeader(IEntityManager ent, EntityUid owner, float range)
    {
        var lookup = ent.System<EntityLookupSystem>();
        var transform = ent.System<SharedTransformSystem>();
        var faction = ent.System<NpcFactionSystem>();
        var mobState = ent.System<MobStateSystem>();

        var leader = owner;

        foreach (var found in lookup.GetEntitiesInRange<NpcFactionMemberComponent>(transform.GetMapCoordinates(owner), range))
        {
            var other = found.Owner;

            if (other == owner || !ent.HasComponent<HTNComponent>(other))
                continue;

            if (!mobState.IsAlive(other) || !faction.IsEntityFriendly(owner, other))
                continue;

            if (other.CompareTo(leader) < 0)
                leader = other;
        }

        return leader;
    }
}
