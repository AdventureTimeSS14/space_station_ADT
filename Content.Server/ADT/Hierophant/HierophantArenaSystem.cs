using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.TileMovement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant; // todo move to shared maybe

public sealed class HierophantArenaSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HierophantSystem _hierophant = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _inside = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HierophantArenaComponent>();

        while (query.MoveNext(out var uid, out var arena))
        {
            if (now < arena.NextCheckAt)
                continue;

            arena.NextCheckAt = now + arena.CheckInterval;
            UpdateArena((uid, arena));
        }
    }

    private void UpdateArena(Entity<HierophantArenaComponent> ent)
    {
        var coords = _transform.GetMapCoordinates(ent.Owner);

        _inside.Clear();

        if (IsFightActive(ent))
        {
            foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.Radius))
            {
                if (!HasComp<InputMoverComponent>(mob.Owner))
                    continue;

                _inside.Add(mob.Owner);

                if (HasComp<TileMovementComponent>(mob.Owner))
                    continue;

                EnsureComp<TileMovementComponent>(mob.Owner);
                EnsureComp<HierophantArenaTileMovementComponent>(mob.Owner);
            }
        }

        var granted = EntityQueryEnumerator<HierophantArenaTileMovementComponent>();
        while (granted.MoveNext(out var uid, out _))
        {
            if (_inside.Contains(uid))
                continue;

            RemCompDeferred<TileMovementComponent>(uid);
            RemCompDeferred<HierophantArenaTileMovementComponent>(uid);
        }
    }

    private bool IsFightActive(Entity<HierophantArenaComponent> ent)
    {
        return true; //DEBUG
        var arenaCoords = _transform.GetMapCoordinates(ent.Owner);
        var query = EntityQueryEnumerator<HierophantComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (_mobState.IsDead(uid))
                continue;

            var bossCoords = _transform.GetMapCoordinates(uid);

            if (bossCoords.MapId != arenaCoords.MapId)
                continue;

            if ((bossCoords.Position - arenaCoords.Position).Length() > ent.Comp.Radius)
                continue;

            if (_hierophant.TryGetTarget(uid, out _))
                return true;
        }

        return false;
    }
}
