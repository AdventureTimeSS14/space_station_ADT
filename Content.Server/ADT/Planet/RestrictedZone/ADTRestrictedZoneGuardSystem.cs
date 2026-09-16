using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Shared.Mobs.Components;
using Content.Shared.Salvage;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Planet.RestrictedZone;

public sealed class ADTRestrictedZoneGuardSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int MaxParentDepth = 8;

    private readonly List<EntityUid> _candidates = new();

    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mobQuery = GetEntityQuery<MobStateComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTRestrictedZoneComponent, RestrictedRangeComponent>();

        while (query.MoveNext(out var uid, out var zone, out var range))
        {
            if (now < zone.NextGuard)
                continue;

            zone.NextGuard = now + zone.GuardInterval;
            Enforce((uid, zone, range));
        }
    }

    private void Enforce(Entity<ADTRestrictedZoneComponent, RestrictedRangeComponent> map)
    {
        _candidates.Clear();

        var mobs = EntityQueryEnumerator<MobStateComponent, TransformComponent>();

        while (mobs.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == map.Owner)
                _candidates.Add(uid);
        }

        var actors = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (actors.MoveNext(out var uid, out var actor, out var xform))
        {
            if (xform.MapUid != map.Owner || _mobQuery.HasComp(uid))
                continue;

            if (_admin.IsAdmin(actor.PlayerSession))
                continue;

            _candidates.Add(uid);
        }

        foreach (var uid in _candidates)
        {
            if (!_xformQuery.TryComp(uid, out var xform))
                continue;

            TryReturn(map, uid, xform);
        }
    }

    private void TryReturn(Entity<ADTRestrictedZoneComponent, RestrictedRangeComponent> map, EntityUid uid, TransformComponent xform)
    {
        if (xform.MapUid != map.Owner)
            return;

        if (xform.GridUid != null && xform.GridUid != map.Owner)
            return;

        var target = uid;
        var targetXform = xform;

        for (var depth = 0; targetXform.ParentUid != map.Owner; depth++)
        {
            if (!targetXform.ParentUid.IsValid() || depth >= MaxParentDepth)
                return;

            target = targetXform.ParentUid;
            targetXform = Transform(target);
        }

        var position = _transform.GetWorldPosition(targetXform);

        if (!TryClamp(map.Comp1, map.Comp2, position, out var clamped))
            return;

        _transform.SetWorldPosition((target, targetXform), clamped);

        if (TryComp<PhysicsComponent>(target, out var physics))
            _physics.SetLinearVelocity(target, Vector2.Zero, body: physics);
    }

    public bool TryClamp(Entity<ADTRestrictedZoneComponent?, RestrictedRangeComponent?> map, Vector2 position, out Vector2 clamped)
    {
        clamped = position;

        if (!Resolve(map.Owner, ref map.Comp1, ref map.Comp2, false))
            return false;

        return TryClamp(map.Comp1, map.Comp2, position, out clamped);
    }

    private static bool TryClamp(
        ADTRestrictedZoneComponent zone,
        RestrictedRangeComponent range,
        Vector2 position,
        out Vector2 clamped)
    {
        clamped = position;

        var offset = position - range.Origin;
        var distance = offset.Length();

        if (distance <= range.Range)
            return false;

        var direction = distance > 0f ? offset / distance : Vector2.UnitY;
        clamped = range.Origin + direction * MathF.Max(0f, range.Range - zone.PushBack);
        return true;
    }
}
