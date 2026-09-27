using Content.Shared.ADT.Drake;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeAttacksSystem : EntitySystem
{
    [Dependency] private readonly ADTDrakeEffectsSystem _effects = default!;
    [Dependency] private readonly ADTDrakeSystem _drake = default!;
    [Dependency] private readonly ADTDrakeSwoopSystem _swoop = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeComponent, ADTDrakeSwoopFinishedEvent>(OnSwoopFinished);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDrakeSequenceComponent, ADTDrakeComponent>();

        while (query.MoveNext(out var uid, out var sequence, out var drake))
        {
            if (sequence.Queue.Count == 0)
                continue;

            if (_mobState.IsDead(uid))
            {
                sequence.Queue.Clear();
                continue;
            }

            for (var i = 0; i < sequence.Queue.Count; i++)
            {
                var step = sequence.Queue[i];

                if (now < step.ExecuteAt)
                    continue;

                sequence.Queue.RemoveAt(i);
                i--;

                ExecuteStep((uid, drake), step);
            }
        }
    }

    private void ExecuteStep(Entity<ADTDrakeComponent> ent, ADTDrakeStep step)
    {
        switch (step.Type)
        {
            case ADTDrakeStepType.MassFireWave:
                MassFireWave(ent, step.Index, step.Count, step.Range);
                break;

            case ADTDrakeStepType.MassFireEnd:
                _drake.SetRecoveryTime(ent, ent.Comp.MassFireEndRecovery);
                break;

            case ADTDrakeStepType.FireCone:
                if (_drake.TryGetTarget(ent, out var target))
                    FireCone(ent, target.Value);
                else
                    _audio.PlayPvs(ent.Comp.FireSound, ent);
                break;

            case ADTDrakeStepType.SetRecovery:
                _drake.SetRecoveryTime(ent, step.Duration);
                break;

            case ADTDrakeStepType.LavaPool:
                LavaPool(ent);
                break;

            case ADTDrakeStepType.EscapeEnrageMassFire:
                MassFire(ent);
                break;

            case ADTDrakeStepType.EscapeEnrageEnd:
                _drake.SetEscapeEnraged(ent, false);
                break;

            case ADTDrakeStepType.MeleeFollowUp:
                _drake.MeleeFollowUp(ent, step.Target);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(step.Type), step.Type, "Unhandled drake step.");
        }
    }

    public void LavaSwoop(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        if (_drake.IsBelowHalfHealth(ent))
        {
            _swoop.TrySwoop(ent, target, true, ent.Comp.LavaSwoopArenaCooldown, ADTDrakeSwoopFollowUp.None);
            return;
        }

        LavaPools(ent, target, ent.Comp.LavaPoolsAmount);

        if (!_swoop.TrySwoop(ent, target, false, ent.Comp.LavaSwoopCooldown, ADTDrakeSwoopFollowUp.LavaSwoopCones))
            LavaSwoopCones(ent);
    }

    private void OnSwoopFinished(Entity<ADTDrakeComponent> ent, ref ADTDrakeSwoopFinishedEvent args)
    {
        if (args.FollowUp == ADTDrakeSwoopFollowUp.LavaSwoopCones)
            LavaSwoopCones(ent);

        if (args.FollowUp == ADTDrakeSwoopFollowUp.LavaPools && args.Target is { } target)
            LavaPools(ent, target, ent.Comp.LavaPoolsAmount);

        if (!args.LavaSuccess)
            ArenaEscapeEnrage(ent);
    }

    public void LavaPools(Entity<ADTDrakeComponent> ent, EntityUid target, int amount)
    {
        _popup.PopupEntity(Loc.GetString("adt-drake-lava-pools"), target, PopupType.LargeCaution);

        var now = _timing.CurTime;

        for (var i = 0; i < amount; i++)
        {
            Enqueue(ent, new ADTDrakeStep
            {
                ExecuteAt = now + ent.Comp.LavaPoolDelay * i,
                Type = ADTDrakeStepType.LavaPool,
            });
        }
    }

    private void LavaPool(Entity<ADTDrakeComponent> ent)
    {
        if (!_drake.TryGetTarget(ent, out var target) || target is not { } victim)
        {
            if (TryComp<ADTDrakeSequenceComponent>(ent, out var sequence))
                sequence.Queue.RemoveAll(s => s.Type == ADTDrakeStepType.LavaPool);

            return;
        }

        var xform = Transform(victim);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var radius = ent.Comp.LavaPoolRadius;
        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var tile = center + new Vector2i(_random.Next(-radius, radius + 1), _random.Next(-radius, radius + 1));

        _effects.SpawnLavaWarning(ent.Comp.LavaWarningProto, gridUid, grid, tile, ent.Comp.LavaPoolResetTime);
    }

    public void ArenaEscapeEnrage(Entity<ADTDrakeComponent> ent)
    {
        _drake.SetRecoveryTime(ent, ent.Comp.EscapeRecovery);
        _popup.PopupEntity(Loc.GetString("adt-drake-escape-enrage", ("drake", ent.Owner)), ent, PopupType.LargeCaution);

        _drake.Heal(ent, ent.Comp.EscapeHeal);
        _drake.SetEscapeEnraged(ent, true);

        var now = _timing.CurTime;
        var massFireAt = now + ent.Comp.EscapeMassFireDelay;

        Enqueue(ent, new ADTDrakeStep
        {
            ExecuteAt = massFireAt,
            Type = ADTDrakeStepType.EscapeEnrageMassFire,
        });

        Enqueue(ent, new ADTDrakeStep
        {
            ExecuteAt = massFireAt + ent.Comp.MassFireWaveDelay * ent.Comp.MassFireTimes,
            Type = ADTDrakeStepType.EscapeEnrageEnd,
        });
    }

    private void LavaSwoopCones(Entity<ADTDrakeComponent> ent)
    {
        var now = _timing.CurTime;

        Enqueue(ent, new ADTDrakeStep
        {
            ExecuteAt = now,
            Type = ADTDrakeStepType.FireCone,
        });

        var end = now;

        if (_drake.IsBelowHalfHealth(ent))
        {
            for (var i = 1; i <= 2; i++)
            {
                end = now + ent.Comp.LavaSwoopConeDelay * i;

                Enqueue(ent, new ADTDrakeStep
                {
                    ExecuteAt = end,
                    Type = ADTDrakeStepType.FireCone,
                });
            }
        }

        Enqueue(ent, new ADTDrakeStep
        {
            ExecuteAt = end,
            Type = ADTDrakeStepType.SetRecovery,
            Duration = ent.Comp.LavaSwoopEndRecovery,
        });
    }

    public void ShootFireAttack(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        if (_drake.IsBelowHalfHealth(ent))
            MassFire(ent);
        else
            FireCone(ent, target);
    }

    public void FireCone(Entity<ADTDrakeComponent> ent, EntityUid target, bool meteors = true)
    {
        _audio.PlayPvs(ent.Comp.FireSound, ent);

        if (meteors && _random.Prob(ent.Comp.FireConeMeteorChance))
            FireRain(ent, target);

        FireConeLines(ent, _transform.GetMapCoordinates(target));
    }

    public void FireConeAt(Entity<ADTDrakeComponent> ent, MapCoordinates target)
    {
        _audio.PlayPvs(ent.Comp.FireSound, ent);
        FireConeLines(ent, target);
    }

    private void FireConeLines(Entity<ADTDrakeComponent> ent, MapCoordinates target)
    {
        if (!TryGetGrid(ent, out var gridUid, out var grid, out var origin))
            return;

        var targetTile = _map.TileIndicesFor(gridUid, grid, target);

        foreach (var offset in ent.Comp.FireConeAngles)
        {
            var tiles = LineTarget(origin, targetTile, offset, ent.Comp.FireLineRange);
            _effects.StartFireLine(ent, gridUid, tiles, ent.Comp);
        }
    }

    public void MassFire(Entity<ADTDrakeComponent> ent)
    {
        var count = ent.Comp.MassFireSpiralCount;
        var range = ent.Comp.MassFireRange;
        var times = ent.Comp.MassFireTimes;
        var now = _timing.CurTime;

        for (var i = 1; i <= times; i++)
        {
            Enqueue(ent, new ADTDrakeStep
            {
                ExecuteAt = now + ent.Comp.MassFireWaveDelay * (i - 1),
                Type = ADTDrakeStepType.MassFireWave,
                Index = i,
                Count = count,
                Range = range,
            });
        }

        Enqueue(ent, new ADTDrakeStep
        {
            ExecuteAt = now + ent.Comp.MassFireWaveDelay * times,
            Type = ADTDrakeStepType.MassFireEnd,
        });
    }

    private void MassFireWave(Entity<ADTDrakeComponent> ent, int wave, int count, int range)
    {
        _drake.SetRecoveryTime(ent, ent.Comp.MassFireWaveRecovery);
        _audio.PlayPvs(ent.Comp.FireSound, ent);

        if (!TryGetGrid(ent, out var gridUid, out var grid, out var origin))
            return;

        var increment = 360f / count;

        for (var j = 1; j <= count; j++)
        {
            var tiles = LineTarget(origin, origin, j * increment + wave * increment / 2f, range);
            _effects.StartFireLine(ent, gridUid, tiles, ent.Comp);
        }
    }

    public void FireRain(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        var xform = Transform(target);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        _popup.PopupEntity(Loc.GetString("adt-drake-fire-rain"), target, PopupType.LargeCaution);

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var radius = ent.Comp.FireRainRadius;

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (!_random.Prob(ent.Comp.FireRainChance))
                    continue;

                var tile = center + new Vector2i(x, y);
                Spawn(ent.Comp.FireRainTargetProto, _map.GridTileToLocal(gridUid, grid, tile));
            }
        }
    }

    public static List<Vector2i> LineTarget(Vector2i origin, Vector2i target, float offset, int range)
    {
        var delta = target - origin;
        var angle = MathF.Atan2(delta.Y, delta.X) + offset * MathF.PI / 180f;

        var end = origin + new Vector2i(
            (int) MathF.Round(MathF.Cos(angle) * range),
            (int) MathF.Round(MathF.Sin(angle) * range));

        var line = GetLine(origin, end);
        line.Remove(origin);
        return line;
    }

    public static List<Vector2i> GetLine(Vector2i from, Vector2i to)
    {
        var result = new List<Vector2i>();

        var x = from.X;
        var y = from.Y;
        var dx = Math.Abs(to.X - from.X);
        var dy = -Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            result.Add(new Vector2i(x, y));

            if (x == to.X && y == to.Y)
                break;

            var e2 = 2 * err;

            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }

        return result;
    }

    private bool TryGetGrid(EntityUid uid, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        var xform = Transform(uid);

        if (xform.GridUid is not { } found || !TryComp(found, out MapGridComponent? foundGrid))
            return false;

        gridUid = found;
        grid = foundGrid;
        tile = _map.TileIndicesFor(found, foundGrid, xform.Coordinates);
        return true;
    }

    private void Enqueue(Entity<ADTDrakeComponent> ent, ADTDrakeStep step)
    {
        var sequence = EnsureComp<ADTDrakeSequenceComponent>(ent);
        sequence.Queue.Add(step);
    }
}
