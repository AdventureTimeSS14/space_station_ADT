using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.TileMovement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Hierophant; // todo move to shared maybe

public sealed class HierophantTileMovementSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HierophantSystem _hierophant = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float CheckInterval = 0.5f;

    private readonly HashSet<EntityUid> _fighting = new();

    private float _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;

        if (_accumulator < CheckInterval)
            return;

        _accumulator = 0f;

        _fighting.Clear();

        var query = EntityQueryEnumerator<HierophantComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!IsFighting(uid))
                continue;

            CollectFighters((uid, comp));
        }

        Apply();
    }

    private bool IsFighting(EntityUid uid)
    {
        if (_mobState.IsDead(uid))
            return false;

        if (HasComp<ActorComponent>(uid))
            return true;

        return _hierophant.TryGetTarget(uid, out _);
    }

    private void CollectFighters(Entity<HierophantComponent> ent)
    {
        var coords = _transform.GetMapCoordinates(ent.Owner);

        if (coords.MapId == MapId.Nullspace)
            return;

        foreach (var mover in _lookup.GetEntitiesInRange<InputMoverComponent>(coords, ent.Comp.TileMovementRadius))
        {
            TryAddFighter(mover.Owner);
        }

        if (_hierophant.TryGetTarget(ent.Owner, out var target))
            TryAddFighter(target.Value);
    }

    private void TryAddFighter(EntityUid uid)
    {
        if (!HasComp<InputMoverComponent>(uid))
            return;

        if (_mobState.IsDead(uid))
            return;

        if (!_fighting.Add(uid))
            return;

        if (TryComp<RiderComponent>(uid, out var rider) && rider.Vehicle != null)
            TryAddFighter(rider.Vehicle.Value);
    }

    private void Apply()
    {
        foreach (var uid in _fighting)
        {
            if (HasComp<TileMovementComponent>(uid))
                continue;

            EnsureComp<TileMovementComponent>(uid);
            EnsureComp<HierophantForcedTileMovementComponent>(uid);
        }

        var granted = EntityQueryEnumerator<HierophantForcedTileMovementComponent>();
        while (granted.MoveNext(out var uid, out _))
        {
            if (_fighting.Contains(uid))
                continue;

            RemCompDeferred<TileMovementComponent>(uid);
            RemCompDeferred<HierophantForcedTileMovementComponent>(uid);
        }
    }
}
