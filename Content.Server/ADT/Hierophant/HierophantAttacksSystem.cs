using System.Numerics;
using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant;

public sealed class HierophantAttacksSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public static readonly Direction[] Cardinals =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    public static readonly Direction[] Diagonals =
    {
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    public static readonly Direction[] AllDirs =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

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

        Enqueue(ent, Step(now, HierophantStepType.Telegraph, destination));
        Enqueue(ent, Step(now, HierophantStepType.Telegraph, source));

        _audio.PlayPvs(ent.Comp.BlinkOutSound, _transform.ToCoordinates(source));
        _audio.PlayPvs(ent.Comp.BlinkInSound, _transform.ToCoordinates(destination));

        var at = now + TimeSpan.FromSeconds(0.2);

        Enqueue(ent, Step(at, HierophantStepType.TelegraphTeleport, destination));
        Enqueue(ent, Step(at, HierophantStepType.TelegraphTeleport, source));

        foreach (var coords in RangeTiles(destination, 1))
        {
            Enqueue(ent, BlastStep(at, coords, ent.Comp.BlinkBlastDamage));
        }

        foreach (var coords in RangeTiles(source, 1))
        {
            Enqueue(ent, BlastStep(at, coords, ent.Comp.BlinkBlastDamage));
        }

        Enqueue(ent, Step(at, HierophantStepType.BlinkFadeOut, destination));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.3), HierophantStepType.BlinkMove, destination));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.4), HierophantStepType.BlinkFadeIn, destination));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.6), HierophantStepType.BlinkFinish, destination));
    }

    public void BlinkSpam(Entity<HierophantComponent> ent, EntityUid target, int blinkCounter, float targetSlowness, bool belowHalfHealth)
    {
        ent.Comp.NextRangedAt = _timing.CurTime + MajorCooldown(ent);

        if ((!belowHalfHealth && !ent.Comp.Enraged) || blinkCounter <= 1)
        {
            Blink(ent, _transform.GetMapCoordinates(target));
            return;
        }

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        Enqueue(ent, SayStep(now, "adt-hierophant-no-escape"));
        Enqueue(ent, Step(now, HierophantStepType.SetAttackColor, default(MapCoordinates)));

        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.4 + targetSlowness);

        for (var i = 0; i < blinkCounter; i++)
        {
            var step = Step(at, HierophantStepType.BlinkFadeOut, default(MapCoordinates));
            step.Target = target;
            step.Amount = 1;
            Enqueue(ent, step);
            at += interval;
        }

        Enqueue(ent, Step(at, HierophantStepType.ClearAttackColor, default(MapCoordinates)));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.8), HierophantStepType.ClearBlinking, default(MapCoordinates)));
    }

    public void CrossBlastSpam(Entity<HierophantComponent> ent, EntityUid target, int crossCounter, float targetSlowness)
    {
        ent.Comp.NextRangedAt = _timing.CurTime + MajorCooldown(ent);

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        Enqueue(ent, SayStep(now, "adt-hierophant-nowhere-to-run"));
        Enqueue(ent, Step(now, HierophantStepType.SetAttackColor, default(MapCoordinates)));

        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.6 + targetSlowness);

        for (var i = 0; i < crossCounter; i++)
        {
            var dirs = _random.Prob(0.6f) ? Cardinals : Diagonals;
            QueueCrossBlast(ent, at, target, dirs);
            at += interval;
        }

        Enqueue(ent, Step(at, HierophantStepType.ClearAttackColor, default(MapCoordinates)));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.8), HierophantStepType.ClearBlinking, default(MapCoordinates)));
    }

    public void ChaserSwarm(Entity<HierophantComponent> ent, EntityUid target, float targetSlowness)
    {
        ent.Comp.NextRangedAt = _timing.CurTime + MajorCooldown(ent);
        ent.Comp.NextChaserAt = _timing.CurTime + ent.Comp.ChaserCooldownTime;

        var sys = EntityManager.System<HierophantSystem>();
        sys.SetBlinking(ent, true);

        var now = _timing.CurTime;
        Enqueue(ent, SayStep(now, "adt-hierophant-cannot-hide"));
        Enqueue(ent, Step(now, HierophantStepType.SetAttackColor, default(MapCoordinates)));

        var targets = GetNearbyTargets(ent, target);
        var cardinals = new List<Direction>(Cardinals);
        var at = now + TimeSpan.FromSeconds(0.6);
        var interval = TimeSpan.FromSeconds(0.8 + targetSlowness);

        while (targets.Count > 0 && cardinals.Count > 0)
        {
            var picked = targets.Count >= cardinals.Count
                ? PickAndTake(targets)
                : _random.Pick(targets);

            var dir = PickAndTake(cardinals);

            var step = Step(at, HierophantStepType.Chaser, default(MapCoordinates));
            step.Target = picked;
            step.Direction = dir;
            step.Speed = ent.Comp.ChaserSpeed;
            step.Amount = 3;
            Enqueue(ent, step);

            at += interval;
        }

        Enqueue(ent, Step(at, HierophantStepType.ClearAttackColor, default(MapCoordinates)));
        Enqueue(ent, Step(at + TimeSpan.FromSeconds(0.8), HierophantStepType.ClearBlinking, default(MapCoordinates)));
    }

    public void Blasts(Entity<HierophantComponent> ent, EntityUid target, Direction[] directions)
    {
        QueueCrossBlast(ent, _timing.CurTime, target, directions);
    }

    private void QueueCrossBlast(Entity<HierophantComponent> ent, TimeSpan at, EntityUid target, Direction[] directions)
    {
        var telegraph = HierophantStepType.Telegraph;

        if (directions == Cardinals)
            telegraph = HierophantStepType.TelegraphCardinal;
        else if (directions == Diagonals)
            telegraph = HierophantStepType.TelegraphDiagonal;

        var telegraphStep = Step(at, telegraph, default(MapCoordinates));
        telegraphStep.Target = target;
        Enqueue(ent, telegraphStep);

        var blastStep = Step(at + TimeSpan.FromSeconds(0.2), HierophantStepType.CrossBlast, default(MapCoordinates));
        blastStep.Target = target;
        blastStep.Directions = new List<Direction>(directions);
        blastStep.Damage = 10f;
        Enqueue(ent, blastStep);
    }

    public void MeleeBlast(Entity<HierophantComponent> ent, MapCoordinates coords)
    {
        var now = _timing.CurTime;
        Enqueue(ent, Step(now, HierophantStepType.Telegraph, coords));
        _audio.PlayPvs(ent.Comp.TelegraphSound, _transform.ToCoordinates(coords));

        foreach (var tile in RangeTiles(coords, 1))
        {
            Enqueue(ent, BlastStep(now + TimeSpan.FromSeconds(0.2), tile, 10f));
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

            foreach (var tile in RingTiles(coords, distance))
            {
                Enqueue(ent, BlastStep(at, tile, 10f));
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

        foreach (var dir in Cardinals)
        {
            var at = now;
            var previous = coords;

            for (var i = 0; i < 10; i++)
            {
                at += TimeSpan.FromSeconds(0.05);
                previous = Offset(previous, dir, 1);

                var step = Step(at, HierophantStepType.Squares, previous);
                step.Direction = dir;
                Enqueue(ent, step);
            }
        }

        foreach (var tile in RingTiles(coords, ent.Comp.ArenaRadius))
        {
            Enqueue(ent, Step(now, HierophantStepType.Wall, tile));
            Enqueue(ent, BlastStep(now, tile, 10f));
        }

        var ourCoords = _transform.GetMapCoordinates(ent.Owner);
        var distance = ChebyshevDistance(ourCoords, coords);

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

    public void ExecuteStep(Entity<HierophantComponent> ent, HierophantStep step)
    {
        var sys = EntityManager.System<HierophantSystem>();

        switch (step.Type)
        {
            case HierophantStepType.Blast:
            {
                var blastCoords = ResolveCoords(step);

                if (blastCoords != null)
                    SpawnBlast(ent, blastCoords.Value, step.Damage, step.FriendlyFireCheck, step.MonsterDamageBoost);

                break;
            }

            case HierophantStepType.Telegraph:
                SpawnTelegraph(ent, step, ent.Comp.TelegraphProto);
                break;

            case HierophantStepType.TelegraphCardinal:
                SpawnTelegraph(ent, step, ent.Comp.TelegraphCardinalProto);
                break;

            case HierophantStepType.TelegraphDiagonal:
                SpawnTelegraph(ent, step, ent.Comp.TelegraphDiagonalProto);
                break;

            case HierophantStepType.TelegraphTeleport:
                SpawnTelegraph(ent, step, ent.Comp.TelegraphTeleportProto);
                break;

            case HierophantStepType.CrossBlast:
                ExecuteCrossBlast(ent, step);
                break;

            case HierophantStepType.Squares:
                ExecuteSquares(ent, step);
                break;

            case HierophantStepType.Chaser:
                ExecuteChaser(ent, step);
                break;

            case HierophantStepType.Wall:
                ExecuteWall(ent, step);
                break;

            case HierophantStepType.BlinkFadeOut:
                ExecuteFadeOut(ent, step, sys);
                break;

            case HierophantStepType.BlinkMove:
                ExecuteBlinkMove(ent, step, sys);
                break;

            case HierophantStepType.BlinkFadeIn:
                Fade(ent.Owner, 0f, 1f);
                break;

            case HierophantStepType.BlinkFinish:
                sys.SetDense(ent.Owner, true);
                sys.SetBlinking(ent, false);
                break;

            case HierophantStepType.SetBlinking:
                sys.SetBlinking(ent, true);
                break;

            case HierophantStepType.ClearBlinking:
                sys.SetBlinking(ent, false);
                break;

            case HierophantStepType.SetAttackColor:
                _appearance.SetData(ent.Owner, HierophantVisuals.Attacking, true);
                break;

            case HierophantStepType.ClearAttackColor:
                _appearance.SetData(ent.Owner, HierophantVisuals.Attacking, false);
                break;

            case HierophantStepType.Say:
                if (step.Message != null)
                    _megafauna.Say(ent.Owner, step.Message);
                break;
                // требует рефактора под полиморфизм
        }
    }

    private void ExecuteCrossBlast(Entity<HierophantComponent> ent, HierophantStep step)
    {
        var coords = ResolveCoords(step);

        if (coords == null)
            return;

        SpawnBlast(ent, coords.Value, step.Damage, step.FriendlyFireCheck, step.MonsterDamageBoost);

        if (step.Directions == null)
            return;

        foreach (var dir in step.Directions)
        {
            for (var i = 1; i <= ent.Comp.BeamRange; i++)
            {
                SpawnBlast(ent, Offset(coords.Value, dir, i), step.Damage, step.FriendlyFireCheck, step.MonsterDamageBoost);
            }
        }
    }

    private void ExecuteSquares(Entity<HierophantComponent> ent, HierophantStep step)
    {
        var squares = Spawn(ent.Comp.SquaresProto, step.Coords);
        _transform.SetLocalRotation(squares, step.Direction.ToAngle());
    }

    private void ExecuteChaser(Entity<HierophantComponent> ent, HierophantStep step)
    {
        if (step.Target == null || TerminatingOrDeleted(step.Target.Value))
            return;

        var chaser = Spawn(ent.Comp.ChaserProto, _transform.GetMoverCoordinates(ent.Owner));

        if (!TryComp<HierophantChaserComponent>(chaser, out var comp))
            return;

        comp.Caster = ent.Owner;
        comp.Target = step.Target;
        comp.Speed = step.Speed > 0f ? step.Speed : ent.Comp.ChaserSpeed;

        if (step.Amount > 0)
        {
            comp.Moving = step.Amount;
            comp.MovingDir = step.Direction;
        }
    }

    private void ExecuteWall(Entity<HierophantComponent> ent, HierophantStep step)
    {
        var wall = Spawn(ent.Comp.WallProto, step.Coords);

        if (TryComp<HierophantWallComponent>(wall, out var wallComp))
            wallComp.Caster = ent.Owner;
    }

    private void ExecuteFadeOut(Entity<HierophantComponent> ent, HierophantStep step, HierophantSystem sys)
    {
        if (step.Amount == 1 && step.Target != null)
        {
            if (TerminatingOrDeleted(step.Target.Value))
                return;

            var targetCoords = _transform.GetMapCoordinates(step.Target.Value);
            var ourCoords = _transform.GetMapCoordinates(ent.Owner);

            if (ChebyshevDistance(ourCoords, targetCoords) < 1)
                return;

            sys.SetBlinking(ent, false);
            Blink(ent, targetCoords);
            sys.SetBlinking(ent, true);
            return;
        }

        Fade(ent.Owner, 1f, 0f);
    }

    private void ExecuteBlinkMove(Entity<HierophantComponent> ent, HierophantStep step, HierophantSystem sys)
    {
        sys.SetDense(ent.Owner, false);
        _transform.SetCoordinates(ent.Owner, step.Coords);
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

    private void SpawnTelegraph(Entity<HierophantComponent> ent, HierophantStep step, string proto)
    {
        var coords = ResolveCoords(step);

        if (coords == null)
            return;

        Spawn(proto, _transform.ToCoordinates(coords.Value));

        if (step.Type is HierophantStepType.TelegraphCardinal or HierophantStepType.TelegraphDiagonal)
            _audio.PlayPvs(ent.Comp.TelegraphSound, _transform.ToCoordinates(coords.Value));
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
        comp.Damage = damage > 0f ? damage : 10f;
        comp.FriendlyFireCheck = friendlyFireCheck;
        comp.MonsterDamageBoost = monsterDamageBoost;

        return blast;
    }

    private MapCoordinates? ResolveCoords(HierophantStep step)
    {
        if (step.Target != null && !TerminatingOrDeleted(step.Target.Value))
            return _transform.GetMapCoordinates(step.Target.Value);

        if (!step.Coords.IsValid(EntityManager))
            return null;

        var coords = _transform.ToMapCoordinates(step.Coords);

        if (coords.MapId == MapId.Nullspace)
            return null;

        return coords;
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

            if (ChebyshevDistance(arenaCoords, coords) <= arena.Radius)
                return true;
        }

        return false;
    }

    private HierophantStep Step(TimeSpan at, HierophantStepType type, MapCoordinates coords)
    {
        var stored = default(EntityCoordinates);

        if (coords.MapId != MapId.Nullspace)
            stored = _transform.ToCoordinates(coords);

        return new HierophantStep
        {
            ExecuteAt = at,
            Type = type,
            Coords = stored,
        };
    }

    private HierophantStep BlastStep(TimeSpan at, MapCoordinates coords, float damage)
    {
        var step = Step(at, HierophantStepType.Blast, coords);
        step.Damage = damage;
        return step;
    }

    private HierophantStep SayStep(TimeSpan at, string message)
    {
        var step = Step(at, HierophantStepType.Say, default(MapCoordinates));
        step.Message = message;
        return step;
    }

    private void Enqueue(Entity<HierophantComponent> ent, HierophantStep step)
    {
        var sequence = EnsureComp<HierophantSequenceComponent>(ent.Owner);
        sequence.Queue.Add(step);
    }

    public static IEnumerable<MapCoordinates> RangeTiles(MapCoordinates center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                yield return new MapCoordinates(center.Position + new Vector2(x, y), center.MapId);
            }
        }
    }

    public static IEnumerable<MapCoordinates> RingTiles(MapCoordinates center, int radius)
    {
        if (radius == 0)
        {
            yield return center;
            yield break;
        }

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                    continue;

                yield return new MapCoordinates(center.Position + new Vector2(x, y), center.MapId);
            }
        }
    }

    private static MapCoordinates Offset(MapCoordinates coords, Direction dir, int distance)
    {
        return new MapCoordinates(coords.Position + dir.ToVec() * distance, coords.MapId);
    }

    public static float ChebyshevDistance(MapCoordinates a, MapCoordinates b)
    {
        if (a.MapId != b.MapId)
            return float.MaxValue;

        var delta = a.Position - b.Position;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
    }
}
