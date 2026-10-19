using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.NPC.Helpers;
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

    public const string IgnoredKey = "ADTIgnoredWeapons";

    [DataField]
    public float Range = 7f;

    [DataField]
    public ADTNpcWeaponRole? OnlyRole;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var containers = _entManager.System<SharedContainerSystem>();

        var ignored = blackboard.TryGetValue<HashSet<EntityUid>>(IgnoredKey, out var stored, _entManager)
            ? stored
            : new HashSet<EntityUid>();

        var ownerPos = transform.GetMapCoordinates(owner).Position;
        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        EntityUid? best = null;
        var bestScore = float.MaxValue;

        var candidates = new List<EntityUid>();

        if (OnlyRole == ADTNpcWeaponRole.Melee)
        {
            foreach (var found in lookup.GetEntitiesInRange<ItemComponent>(ownerCoords, Range))
                candidates.Add(found.Owner);
        }
        else
        {
            foreach (var found in lookup.GetEntitiesInRange<GunComponent>(ownerCoords, Range))
                candidates.Add(found.Owner);
        }

        foreach (var gun in candidates)
        {
            if (ignored.Contains(gun) || !_entManager.HasComponent<ItemComponent>(gun))
                continue;

            if (containers.IsEntityInContainer(gun))
                continue;

            var role = ADTNpcWeaponHelper.Classify(_entManager, gun);

            if (OnlyRole != null && role != OnlyRole)
                continue;

            if (OnlyRole != ADTNpcWeaponRole.Melee)
            {
                ADTNpcWeaponHelper.GetAmmo(_entManager, gun, out var count, out _);

                if (count <= 0)
                    continue;
            }

            var score = Vector2.Distance(ownerPos, transform.GetMapCoordinates(gun).Position);

            if (role != ADTNpcWeaponRole.Main)
                score += 100f;

            if (score >= bestScore)
                continue;

            best = gun;
            bestScore = score;
        }

        if (best is not { } target)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
        }));
    }
}
