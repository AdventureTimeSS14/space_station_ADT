using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
// using Content.Shared.NarcoticEffects.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Changeling.Components;
using Robust.Shared.Timing;

namespace Content.Server.Changeling;

public sealed class LingHallucinationSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LingHallucinationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LingHallucinationComponent, ComponentShutdown>(OnShutdown);
    }


    private void OnMapInit(EntityUid uid, LingHallucinationComponent component, MapInitEvent args)
    {
        // Allow this entity to be seen by other ghosts.
        var visibility = EnsureComp<VisibilityComponent>(uid);

        _visibilitySystem.RemoveLayer((uid, visibility), (ushort) VisibilityFlags.Normal, false);
        _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
    }

    private void OnShutdown(EntityUid uid, LingHallucinationComponent component, ComponentShutdown args)
    {
        // Perf: If the entity is deleting itself, no reason to change these back.
        if (Terminating(uid))
            return;

        // Entity can't be seen by ghosts anymore.
        if (TryComp(uid, out VisibilityComponent? visibility))
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (ushort) component.Layer, false);
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
                return;

            _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~component.Layer, eye);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LingHallucinationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stat, out var xform))
        {
            TryComp<VisibilityComponent>(uid, out var curVisibility);
            if (curVisibility != null)
            {
                if (stat.Layer == curVisibility.Layer)
                    return;
            }
            // Allow this entity to be seen by other ghosts.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            _visibilitySystem.AddLayer((uid, visibility), (ushort) stat.Layer, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
                return;

            _eye.SetVisibilityMask(uid, eye.VisibilityMask | stat.Layer, eye);
            _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
        }
    }

}
