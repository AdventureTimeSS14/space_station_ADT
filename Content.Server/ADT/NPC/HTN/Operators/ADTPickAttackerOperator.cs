using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickAttackerOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private SharedTransformSystem _transform = default!;
    private MobStateSystem _mobState = default!;
    private NpcFactionSystem _faction = default!;

    [DataField]
    public float Seconds = 3f;

    [DataField]
    public float MaxRange = 30f;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _mobState = sysManager.GetEntitySystem<MobStateSystem>();
        _faction = sysManager.GetEntitySystem<NpcFactionSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(owner, out ADTNpcCombatMemoryComponent? memory) ||
            memory.LastAttacker is not { } attacker ||
            _entManager.Deleted(attacker) ||
            (_timing.CurTime - memory.LastHurt).TotalSeconds > Seconds ||
            _mobState.IsIncapacitated(attacker) ||
            _faction.IsEntityFriendly(owner, attacker))
        {
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));
        }

        var ownerPos = _transform.GetMapCoordinates(owner);
        var attackerPos = _transform.GetMapCoordinates(attacker);

        if (ownerPos.MapId != attackerPos.MapId ||
            Vector2.Distance(ownerPos.Position, attackerPos.Position) > MaxRange)
        {
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));
        }

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, attacker },
            { KeyCoordinates, new EntityCoordinates(attacker, Vector2.Zero) },
        }));
    }
}
