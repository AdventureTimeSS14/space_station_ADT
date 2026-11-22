using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;
    private EntityLookupSystem _lookup = default!;
    private SharedTransformSystem _transform = default!;
    private SharedContainerSystem _containers = default!;

    private readonly HashSet<Entity<ItemComponent>> _items = new();
    private readonly HashSet<Entity<GunComponent>> _guns = new();

    [DataField]
    public float Range = 7f;

    [DataField]
    public ADTNpcWeaponRole? OnlyRole;

    [DataField]
    public float SearchCooldown = 3f;

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
        _containers = sysManager.GetEntitySystem<SharedContainerSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_gear.CanSearchWeapon(owner, OnlyRole))
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        var best = OnlyRole == ADTNpcWeaponRole.Melee
            ? FindBest(owner, blackboard, _items)
            : FindBest(owner, blackboard, _guns);

        if (best is not { } target)
        {
            _gear.DelayWeaponSearch(owner, OnlyRole, TimeSpan.FromSeconds(SearchCooldown));
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));
        }

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
        }));
    }

    private EntityUid? FindBest<T>(EntityUid owner, NPCBlackboard blackboard, HashSet<Entity<T>> candidates) where T : IComponent
    {
        var ignored = ADTNpcGearSystem.GetIgnored(blackboard, ADTNpcGearSystem.IgnoredWeaponsKey, _entManager);
        var ownerXform = _entManager.GetComponent<TransformComponent>(owner);
        var ownerPos = _transform.GetMapCoordinates(owner, ownerXform).Position;

        EntityUid? best = null;
        var bestScore = float.MaxValue;

        candidates.Clear();
        _lookup.GetEntitiesInRange(ownerXform.Coordinates, Range, candidates);

        foreach (var candidate in candidates)
        {
            var item = candidate.Owner;

            if (ignored != null && ignored.Contains(item))
                continue;

            if (!_entManager.HasComponent<ItemComponent>(item) || _containers.IsEntityInContainer(item))
                continue;

            var role = _gear.Classify(item);

            if (OnlyRole != null && role != OnlyRole)
                continue;

            if (OnlyRole != ADTNpcWeaponRole.Melee)
            {
                _gear.GetAmmo(item, out var count, out _);

                if (count <= 0)
                    continue;
            }

            var score = Vector2.Distance(ownerPos, _transform.GetMapCoordinates(item).Position);

            if (role != ADTNpcWeaponRole.Main)
                score += 100f;

            if (score >= bestScore)
                continue;

            best = item;
            bestScore = score;
        }

        candidates.Clear();
        return best;
    }
}
