using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTHostileNearPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Range = 10f;

    [DataField]
    public bool Invert;

    private static GameTick _cacheTick;
    private static readonly Dictionary<(EntityUid, float), bool> Cache = new();

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_timing.CurTick != _cacheTick)
        {
            Cache.Clear();
            _cacheTick = _timing.CurTick;
        }

        if (!Cache.TryGetValue((owner, Range), out var near))
        {
            var mobState = _entManager.System<MobStateSystem>();

            foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
            {
                if (mobState.IsIncapacitated(hostile))
                    continue;

                near = true;
                break;
            }

            Cache[(owner, Range)] = near;
        }

        return near ^ Invert;
    }
}
