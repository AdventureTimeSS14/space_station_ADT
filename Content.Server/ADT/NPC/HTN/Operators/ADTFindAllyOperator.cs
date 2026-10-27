using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindAllyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;
    private EntityLookupSystem _lookup = default!;
    private SharedTransformSystem _transform = default!;
    private NpcFactionSystem _faction = default!;
    private MobStateSystem _mobState = default!;

    private readonly HashSet<Entity<NpcFactionMemberComponent>> _found = new();

    [DataField]
    public float Range = 15f;

    [DataField]
    public float MinDistance;

    [DataField]
    public List<EntProtoId> Prototypes = new();

    [DataField]
    public ComponentRegistry? Components;

    [DataField]
    public float MinDamage;

    [DataField]
    public bool AllowCritical;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _faction = sysManager.GetEntitySystem<NpcFactionSystem>();
        _mobState = sysManager.GetEntitySystem<MobStateSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var position = _transform.GetMapCoordinates(owner);

        EntityUid? best = null;
        var bestScore = float.MaxValue;

        _found.Clear();
        _lookup.GetEntitiesInRange(position, Range, _found);

        foreach (var found in _found)
        {
            var other = found.Owner;

            if (other == owner || !_faction.IsEntityFriendly(owner, other))
                continue;

            if (AllowCritical ? _mobState.IsDead(other) : !_mobState.IsAlive(other))
                continue;

            if (Prototypes.Count > 0 && !_gear.MatchesPrototype(other, Prototypes))
                continue;

            if (Components != null && !HasComponents(other, Components))
                continue;

            var distance = Vector2.Distance(position.Position, _transform.GetMapCoordinates(other).Position);

            if (distance <= MinDistance)
                return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

            var score = distance;

            if (MinDamage > 0f)
            {
                var damage = _gear.DamageFraction(other);

                if (damage < MinDamage)
                    continue;

                score = -damage;
            }

            if (score >= bestScore)
                continue;

            best = other;
            bestScore = score;
        }

        if (best == null)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, best.Value },
            { KeyCoordinates, new EntityCoordinates(best.Value, Vector2.Zero) },
        }));
    }

    private bool HasComponents(EntityUid uid, ComponentRegistry components)
    {
        foreach (var entry in components.Values)
        {
            if (!_entManager.HasComponent(uid, entry.Component.GetType()))
                return false;
        }

        return true;
    }
}
