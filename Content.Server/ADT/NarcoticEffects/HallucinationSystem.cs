using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Content.Shared.NarcoticEffects.Components;
using Robust.Shared.Physics.Systems;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.Hallucination;

public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HallucinationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HallucinationComponent, ComponentShutdown>(OnShutdown);
    }


    private void OnMapInit(EntityUid uid, HallucinationComponent component, MapInitEvent args)
    {
        // Allow this entity to be seen by other ghosts.
        var visibility = EnsureComp<VisibilityComponent>(uid);

        if (_ticker.RunLevel != GameRunLevel.PostRound)
        {
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Narcotic, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
                return;

            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) VisibilityFlags.Narcotic, eye);
        }
    }

    private void OnShutdown(EntityUid uid, HallucinationComponent component, ComponentShutdown args)
    {
        // Perf: If the entity is deleting itself, no reason to change these back.
        if (Terminating(uid))
            return;

        // Entity can't be seen by ghosts anymore.
        if (TryComp(uid, out VisibilityComponent? visibility))
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Narcotic, false);
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
                return;

            _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(int) VisibilityFlags.Narcotic, eye);
        }
    }
}
