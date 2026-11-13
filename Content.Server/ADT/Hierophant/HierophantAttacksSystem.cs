using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant;

public sealed class HierophantAttacksSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public void Blink(Entity<HierophantComponent> ent, MapCoordinates destination)
    {
        if (ent.Comp.Blinking)
            return;

        var source = _transform.GetMapCoordinates(ent.Owner);

        if (source.MapId != destination.MapId)
            return;

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;

        QueueTelegraph(ent, now, comp => comp.TelegraphProto, destination, null, false);
        QueueTelegraph(ent, now, comp => comp.TelegraphProto, source, null, false);

        _audio.PlayPvs(ent.Comp.BlinkOutSound, _transform.ToCoordinates(source));
        _audio.PlayPvs(ent.Comp.BlinkInSound, _transform.ToCoordinates(destination));

        var at = now + TimeSpan.FromSeconds(0.2);

        QueueTelegraph(ent, at, comp => comp.TelegraphTeleportProto, destination, null, false);
        QueueTelegraph(ent, at, comp => comp.TelegraphTeleportProto, source, null, false);

        foreach (var coords in ADTMegafaunaGeometry.RangeTiles(destination, 1))
        {
            QueueBlast(ent, at, coords, ent.Comp.BlinkBlastDamage);
        }

        foreach (var coords in ADTMegafaunaGeometry.RangeTiles(source, 1))
        {
            QueueBlast(ent, at, coords, ent.Comp.BlinkBlastDamage);
        }

        var stored = Store(destination);

        Schedule(ent, at, () => Fade(ent.Owner, 1f, 0f));
        Schedule(ent, at + TimeSpan.FromSeconds(0.3), () =>
        {
            sys.SetDense(ent.Owner, false);
            _transform.SetCoordinates(ent.Owner, stored);
        });
        Schedule(ent, at + TimeSpan.FromSeconds(0.4), () => Fade(ent.Owner, 0f, 1f));
        Schedule(ent, at + TimeSpan.FromSeconds(0.6), () =>
        {
            sys.SetDense(ent.Owner, true);
            sys.SetBlinking(ent, false);
        });
    }

    public void BlinkSpam(Entity<HierophantComponent> ent, EntityUid target, int blinkCounter, float targetSlowness, bool belowHalfHealth)
    {
        SetRangedCooldown(ent, MajorCooldown(ent));

        if ((!belowHalfHealth && !ent.Comp.Enraged) || blinkCounter <= 1)
        {
            Blink(ent, _transform.GetMapCoordinates(target));
            return;
        }

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        QueueSay(ent, now, "adt-hierophant-no-escape");
        QueueAttackColor(ent, now, true);

        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.4 + targetSlowness);

        for (var i = 0; i < blinkCounter; i++)
        {
            Schedule(ent, at, () => BlinkTowards(ent, target, sys));
            at += interval;
        }

        QueueAttackColor(ent, at, false);
        Schedule(ent, at + TimeSpan.FromSeconds(0.8), () => sys.SetBlinking(ent, false));
    }

    public void CrossBlastSpam(Entity<HierophantComponent> ent, EntityUid target, int crossCounter, float targetSlowness)
    {
        SetRangedCooldown(ent, MajorCooldown(ent));

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        QueueSay(ent, now, "adt-hierophant-nowhere-to-run");
        QueueAttackColor(ent, now, true);

        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.6 + targetSlowness);

        for (var i = 0; i < crossCounter; i++)
        {
            var dirs = _random.Prob(0.6f) ? ADTMegafaunaGeometry.Cardinals : ADTMegafaunaGeometry.Diagonals;
            QueueCrossBlast(ent, at, target, dirs);
            at += interval;
        }

        QueueAttackColor(ent, at, false);
        Schedule(ent, at + TimeSpan.FromSeconds(0.8), () => sys.SetBlinking(ent, false));
    }

    public void ChaserSwarm(Entity<HierophantComponent> ent, EntityUid target, float targetSlowness)
    {
        SetRangedCooldown(ent, MajorCooldown(ent));
        ent.Comp.NextChaserAt = _timing.CurTime + ent.Comp.ChaserCooldownTime;

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        QueueSay(ent, now, "adt-hierophant-cannot-hide");
        QueueAttackColor(ent, now, true);

        var targets = GetNearbyTargets(ent, target);
        var cardinals = new List<Direction>(ADTMegafaunaGeometry.Cardinals);
        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.8 + targetSlowness);

        while (targets.Count > 0 && cardinals.Count > 0)
        {
            var picked = targets.Count >= cardinals.Count
                ? PickAndTake(targets)
                : _random.Pick(targets);

            var dir = PickAndTake(cardinals);
            var speed = ent.Comp.ChaserSpeed;

            Schedule(ent, at, () => SpawnScheduledChaser(ent, picked, speed, 3, dir));

            at += interval;
        }

        QueueAttackColor(ent, at, false);
        Schedule(ent, at + TimeSpan.FromSeconds(0.8), () => sys.SetBlinking(ent, false));
    }

    public void Blasts(Entity<HierophantComponent> ent, EntityUid target, Direction[] directions)
    {
        QueueCrossBlast(ent, _timing.CurTime, target, directions);
    }

    private void QueueCrossBlast(Entity<HierophantComponent> ent, TimeSpan at, EntityUid target, Direction[] directions)
    {
        if (directions == ADTMegafaunaGeometry.Cardinals)
            QueueTelegraph(ent, at, comp => comp.TelegraphCardinalProto, null, target, true);
        else if (directions == ADTMegafaunaGeometry.Diagonals)
            QueueTelegraph(ent, at, comp => comp.TelegraphDiagonalProto, null, target, true);
        else
            QueueTelegraph(ent, at, comp => comp.TelegraphProto, null, target, false);

        var dirs = new List<Direction>(directions);
        var damage = ent.Comp.BlastDamage;

        Schedule(ent, at + TimeSpan.FromSeconds(0.2), () => CrossBlast(ent, target, dirs, damage));
    }

    public void MeleeBlast(Entity<HierophantComponent> ent, MapCoordinates coords)
    {
        var now = _timing.CurTime;
        QueueTelegraph(ent, now, comp => comp.TelegraphProto, coords, null, false);
        _audio.PlayPvs(ent.Comp.TelegraphSound, _transform.ToCoordinates(coords));

        foreach (var tile in ADTMegafaunaGeometry.RangeTiles(coords, 1))
        {
            QueueBlast(ent, now + TimeSpan.FromSeconds(0.2), tile, ent.Comp.BlastDamage);
        }
    }

    public void Burst(Entity<HierophantComponent> ent, EntityCoordinates origin, int? rangeOverride = null, float spreadSpeed = 0.5f)
    {
        var coords = _transform.ToMapCoordinates(origin);

        if (coords.MapId == MapId.Nullspace)
            return;

        var range = rangeOverride ?? ent.Comp.BurstRange;
        _audio.PlayPvs(ent.Comp.BlinkInSound, origin);

        var at = _timing.CurTime;

        for (var distance = 0; distance <= range; distance++)
        {
            if (distance > 0)
            {
                // deciseconds in SS13: 1 + min(burst_range - dist, 12) * spread_speed
                var delay = 0.1f + Math.Min(range - distance, 12) * spreadSpeed * 0.1f;
                at += TimeSpan.FromSeconds(delay);
            }

            foreach (var tile in ADTMegafaunaGeometry.RingTiles(coords, distance))
            {
                QueueBlast(ent, at, tile, ent.Comp.BlastDamage);
            }
        }
    }

    public void ArenaTrap(Entity<HierophantComponent> ent, EntityUid victim, bool forced = false)
    {
        if (TerminatingOrDeleted(victim) || _mobState.IsDead(victim))
            return;

        var now = _timing.CurTime;

        if (!forced && now < ent.Comp.NextArenaAt)
            return;

        var coords = _transform.GetMapCoordinates(victim);

        if (coords.MapId == MapId.Nullspace)
            return;

        if (!forced && IsInsideArena(coords) && victim != ent.Owner)
            return;

        ent.Comp.NextArenaAt = now + ent.Comp.ArenaCooldownTime;

        foreach (var dir in ADTMegafaunaGeometry.Cardinals)
        {
            var at = now;
            var previous = coords;

            for (var i = 0; i < 10; i++)
            {
                at += TimeSpan.FromSeconds(0.05);
                previous = ADTMegafaunaGeometry.Offset(previous, dir, 1);

                var stored = Store(previous);
                Schedule(ent, at, () => SpawnSquares(ent, stored, dir));
            }
        }

        foreach (var tile in ADTMegafaunaGeometry.RingTiles(coords, ent.Comp.ArenaRadius))
        {
            var stored = Store(tile);
            Schedule(ent, now, () => SpawnWall(ent, stored));
            QueueBlast(ent, now, tile, ent.Comp.BlastDamage);
        }

        var ourCoords = _transform.GetMapCoordinates(ent.Owner);
        var distance = ADTMegafaunaGeometry.ChebyshevDistance(ourCoords, coords);

        if (distance >= ent.Comp.ArenaRadius || forced)
            Blink(ent, coords);
    }

    public void CollapseWalls(EntityUid caster)
    {
        var query = EntityQueryEnumerator<HierophantWallComponent>();

        while (query.MoveNext(out var uid, out var wall))
        {
            if (wall.Caster != caster)
                continue;

            QueueDel(uid);
        }
    }

    private void QueueBlast(Entity<HierophantComponent> ent, TimeSpan at, MapCoordinates coords, float damage)
    {
        var stored = Store(coords);

        Schedule(ent, at, () =>
        {
            if (ResolveCoords(null, stored) is { } blastCoords)
                SpawnBlast(ent, blastCoords, damage);
        });
    }

    private void QueueTelegraph(
        Entity<HierophantComponent> ent,
        TimeSpan at,
        Func<HierophantComponent, EntProtoId> proto,
        MapCoordinates? coords,
        EntityUid? target,
        bool sound)
    {
        var stored = coords is { } mapCoords ? Store(mapCoords) : default;

        Schedule(ent, at, () =>
        {
            if (ResolveCoords(target, stored) is not { } telegraphCoords)
                return;

            var spawnCoords = _transform.ToCoordinates(telegraphCoords);
            Spawn(proto(ent.Comp), spawnCoords);

            if (sound)
                _audio.PlayPvs(ent.Comp.TelegraphSound, spawnCoords);
        });
    }

    private void QueueSay(Entity<HierophantComponent> ent, TimeSpan at, string message)
    {
        Schedule(ent, at, () => _megafauna.Say(ent.Owner, message));
    }

    private void QueueAttackColor(Entity<HierophantComponent> ent, TimeSpan at, bool attacking)
    {
        Schedule(ent, at, () => _appearance.SetData(ent.Owner, HierophantVisuals.Attacking, attacking));
    }

    private void CrossBlast(Entity<HierophantComponent> ent, EntityUid target, List<Direction> directions, float damage)
    {
        if (ResolveCoords(target, default) is not { } coords)
            return;

        SpawnBlast(ent, coords, damage);

        foreach (var dir in directions)
        {
            for (var i = 1; i <= ent.Comp.BeamRange; i++)
            {
                SpawnBlast(ent, ADTMegafaunaGeometry.Offset(coords, dir, i), damage);
            }
        }
    }

    private void SpawnSquares(Entity<HierophantComponent> ent, EntityCoordinates coords, Direction dir)
    {
        var squares = Spawn(ent.Comp.SquaresProto, coords);
        _transform.SetLocalRotation(squares, dir.ToAngle());
    }

    private void SpawnWall(Entity<HierophantComponent> ent, EntityCoordinates coords)
    {
        var wall = Spawn(ent.Comp.WallProto, coords);

        if (TryComp<HierophantWallComponent>(wall, out var wallComp))
            wallComp.Caster = ent.Owner;
    }

    private void SpawnScheduledChaser(Entity<HierophantComponent> ent, EntityUid target, float speed, int moving, Direction dir)
    {
        if (TerminatingOrDeleted(target))
            return;

        var chaser = Spawn(ent.Comp.ChaserProto, _transform.GetMoverCoordinates(ent.Owner));

        if (!TryComp<HierophantChaserComponent>(chaser, out var comp))
            return;

        comp.Caster = ent.Owner;
        comp.Target = target;
        comp.Speed = speed > 0f ? speed : ent.Comp.ChaserSpeed;
        comp.Damage = ent.Comp.BlastDamage;
        comp.Moving = moving;
        comp.MovingDir = dir;
    }

    private void BlinkTowards(Entity<HierophantComponent> ent, EntityUid target, HierophantSystem sys)
    {
        if (TerminatingOrDeleted(target))
            return;

        var targetCoords = _transform.GetMapCoordinates(target);
        var ourCoords = _transform.GetMapCoordinates(ent.Owner);

        if (ADTMegafaunaGeometry.ChebyshevDistance(ourCoords, targetCoords) < 1)
            return;

        sys.SetBlinking(ent, false);
        Blink(ent, targetCoords);
        sys.SetBlinking(ent, true);
    }

    private void Fade(EntityUid uid, float from, float to)
    {
        var fade = EnsureComp<HierophantFadeComponent>(uid);
        fade.StartAlpha = from;
        fade.EndAlpha = to;
        fade.StartTime = _timing.CurTime;
        fade.Duration = TimeSpan.FromSeconds(0.2);
        Dirty(uid, fade);
    }

    public EntityUid? SpawnBlast(
        Entity<HierophantComponent> ent,
        MapCoordinates coords,
        float damage,
        bool friendlyFireCheck = false,
        bool monsterDamageBoost = true)
    {
        if (coords.MapId == MapId.Nullspace)
            return null;

        var blast = Spawn(ent.Comp.BlastProto, coords);

        if (!TryComp<HierophantBlastComponent>(blast, out var comp))
            return blast;

        comp.Caster = ent.Owner;
        comp.Damage = damage > 0f ? damage : ent.Comp.BlastDamage;
        comp.FriendlyFireCheck = friendlyFireCheck;
        comp.MonsterDamageBoost = monsterDamageBoost;

        return blast;
    }

    private MapCoordinates? ResolveCoords(EntityUid? target, EntityCoordinates stored)
    {
        if (target != null && !TerminatingOrDeleted(target.Value))
            return _transform.GetMapCoordinates(target.Value);

        if (!stored.IsValid(EntityManager))
            return null;

        var coords = _transform.ToMapCoordinates(stored);

        if (coords.MapId == MapId.Nullspace)
            return null;

        return coords;
    }

    private EntityCoordinates Store(MapCoordinates coords)
    {
        if (coords.MapId == MapId.Nullspace)
            return default;

        return _transform.ToCoordinates(coords);
    }

    private void Schedule(Entity<HierophantComponent> ent, TimeSpan at, Action action)
    {
        _megafauna.ScheduleAt(ent.Owner, at, action);
    }

    private void SetRangedCooldown(Entity<HierophantComponent> ent, TimeSpan cooldown)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRangedCooldown((ent.Owner, ai), cooldown);
    }

    private List<EntityUid> GetNearbyTargets(Entity<HierophantComponent> ent, EntityUid fallback)
    {
        var results = new List<EntityUid>();
        var coords = _transform.GetMapCoordinates(ent.Owner);
        var candidates = _lookup.GetEntitiesInRange<MobStateComponent>(coords, 21f);

        foreach (var candidate in candidates)
        {
            if (candidate.Owner == ent.Owner)
                continue;

            if (_mobState.IsDead(candidate.Owner))
                continue;

            if (_npcFaction.IsEntityFriendly(ent.Owner, candidate.Owner))
                continue;

            results.Add(candidate.Owner);
        }

        if (results.Count == 0 && !TerminatingOrDeleted(fallback))
            results.Add(fallback);

        return results;
    }

    private T PickAndTake<T>(List<T> list)
    {
        var index = _random.Next(list.Count);
        var value = list[index];
        list.RemoveAt(index);
        return value;
    }

    private TimeSpan MajorCooldown(Entity<HierophantComponent> ent)
    {
        var seconds = Math.Max(0.5, ent.Comp.MajorAttackCooldown.TotalSeconds - ent.Comp.AngerModifier * 0.075);
        return TimeSpan.FromSeconds(seconds);
    }

    private bool IsInsideArena(MapCoordinates coords)
    {
        var query = EntityQueryEnumerator<HierophantArenaComponent>();

        while (query.MoveNext(out var uid, out var arena))
        {
            var arenaCoords = _transform.GetMapCoordinates(uid);

            if (arenaCoords.MapId != coords.MapId)
                continue;

            if (ADTMegafaunaGeometry.ChebyshevDistance(arenaCoords, coords) <= arena.Radius)
                return true;
        }

        return false;
    }
}
