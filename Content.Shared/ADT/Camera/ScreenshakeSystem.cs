using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Camera;

public sealed class ScreenshakeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    /// Cooldowns take a string key so that multiple systems can apply their shake effects without one shake effect blocking the other.
    /// </summary>
    private readonly Dictionary<EntityUid, Dictionary<string, TimeSpan>> _shakeCooldowns = [];

    // Reused across GetEye* callbacks — allocating FastNoiseLite + reading CVars every frame was a hot path in combat.
    private readonly FastNoiseLite _offsetNoise = new(67);
    private readonly FastNoiseLite _rotationNoise = new(67 + 420);
    private float _shakeIntensity = 1f;
    private readonly List<ScreenshakeCommand> _expiredScratch = [];

    #region Internal

    public override void Initialize()
    {
        base.Initialize();

        _offsetNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _rotationNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        if (_cfg.IsCVarRegistered(CCVars.ScreenShakeIntensity.Name))
            _cfg.OnValueChanged(CCVars.ScreenShakeIntensity, v => _shakeIntensity = v, true);

        SubscribeLocalEvent<ScreenshakeComponent, GetEyeRotationEvent>(OnGetEyeRotation);
        SubscribeLocalEvent<ScreenshakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeLocalEvent<ScreenshakeComponent, EntityUnpausedEvent>(OnEntityUnpaused);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shakers = EntityQueryEnumerator<EyeComponent, ScreenshakeComponent>();
        var now = _timing.CurTime;

        while (shakers.MoveNext(out var uid, out _, out var shake))
        {
            if (shake.Commands.Count == 0)
            {
                RemCompDeferred<ScreenshakeComponent>(uid);
                continue;
            }

            _expiredScratch.Clear();
            foreach (var command in shake.Commands)
            {
                if (now >= command.CalculatedEnd)
                    _expiredScratch.Add(command);
            }

            if (_expiredScratch.Count == 0)
                continue;

            foreach (var command in _expiredScratch)
                shake.Commands.Remove(command);

            Dirty(uid, shake);
        }
    }

    private void OnGetEyeOffset(EntityUid uid, ScreenshakeComponent shake, ref GetEyeOffsetEvent args)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var accumulatedOffset = Vector2.Zero;
        var maxOffset = new Vector2(0.15f, 0.15f);
        var timeMs = (float) _timing.RealTime.TotalMilliseconds;

        foreach (var command in shake.Commands)
        {
            if (command.Translational is null)
                continue;

            var trauma = CalculateTraumaValueForCurrentTime(command.Translational, command.Start) * _shakeIntensity;
            if (trauma <= 0)
                continue;

            _offsetNoise.SetFrequency(command.Translational.Frequency);
            _offsetNoise.SetSeed(67);
            var startMs = (float) command.Start.TotalMilliseconds;
            var offX = (maxOffset.X * trauma) * _offsetNoise.GetNoise(timeMs, startMs);
            _offsetNoise.SetSeed(68);
            var offY = (maxOffset.Y * trauma) * _offsetNoise.GetNoise(timeMs, startMs);
            accumulatedOffset += new Vector2(offX, offY);
        }

        args.Offset += accumulatedOffset;
    }

    private void OnGetEyeRotation(EntityUid uid, ScreenshakeComponent shake, ref GetEyeRotationEvent args)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var accumulatedAngle = Angle.Zero;
        const float maxAngleDegrees = 20f;
        var timeMs = (float) _timing.RealTime.TotalMilliseconds;

        foreach (var command in shake.Commands)
        {
            if (command.Rotational == null)
                continue;

            var trauma = CalculateTraumaValueForCurrentTime(command.Rotational, command.Start) * _shakeIntensity;
            if (trauma <= 0)
                continue;

            _rotationNoise.SetFrequency(command.Rotational.Frequency);
            var angle = (maxAngleDegrees * trauma) * _rotationNoise.GetNoise(timeMs, (float) command.Start.TotalMilliseconds);
            accumulatedAngle += Angle.FromDegrees(angle);
        }

        // TODO ughhh this shit breaks with something idk
        // TODO: ^find whatever that is and fix it
        args.Rotation += accumulatedAngle;
    }

    private void OnEntityUnpaused(EntityUid uid, ScreenshakeComponent shake, ref EntityUnpausedEvent args)
    {
        // rebuild screenshake commands but with offset times
        var newSet = new HashSet<ScreenshakeCommand>();
        foreach (var command in shake.Commands)
        {
            var newCommand = command with
            {
                CalculatedEnd = command.CalculatedEnd + args.PausedTime,
                Start = command.Start + args.PausedTime,
            };

            newSet.Add(newCommand);
        }

        shake.Commands = newSet;
        Dirty(uid, shake);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
        => _shakeCooldowns.Clear();

    /// <summary>
    /// Calculates when both traumas will be at least = 0 given the decay rate and start time.
    /// </summary>
    private TimeSpan CalculateEndTimeForCommand(Entity<ScreenshakeComponent> ent, ScreenshakeParameters? translation, ScreenshakeParameters? rotation, TimeSpan start)
    {
        // https://www.desmos.com/calculator/optip8eucx
        var secsUntilRotationalEnd = rotation != null ? MathF.Sqrt(rotation.Trauma / rotation.DecayRate) : 0f;
        var secsUntilTranslationalEnd = translation != null ? MathF.Sqrt(translation.Trauma / translation.DecayRate) : 0f;
        var larger = secsUntilTranslationalEnd >= secsUntilRotationalEnd
            ? secsUntilTranslationalEnd
            : secsUntilRotationalEnd;

        return start + TimeSpan.FromSeconds(larger);
    }

    /// <summary>
    /// Gets the trauma value for the current time, given the decay rate and start time.
    /// </summary>
    private float CalculateTraumaValueForCurrentTime(ScreenshakeParameters parameters, TimeSpan start)
    {
        var timeDiff = _timing.CurTime - start;

        // erm
        if (timeDiff < TimeSpan.Zero)
            return 0f;

        // trauma decays quadratically with seconds passed
        // https://www.desmos.com/calculator/optip8eucx
        var totalSecsSquared = (float) (timeDiff.TotalSeconds * timeDiff.TotalSeconds);
        return (-totalSecsSquared * parameters.DecayRate) + parameters.Trauma;
    }
    #endregion

    #region Public API

    public bool IsOnCooldown(EntityUid uid, string key)
    {
        if (!_shakeCooldowns.TryGetValue(uid, out var cooldowns))
        {
            _shakeCooldowns.Add(uid, []);
            return false;
        }
        if (!cooldowns.TryGetValue(key, out var cooldown)) return false;
        if (_timing.CurTime < cooldown) return true;
        _shakeCooldowns[uid].Remove(key); // remove from cooldowns if it shouldn't be on cooldown anymore
        return false;
    }

    public void Screenshake(EntityUid uid, ScreenshakeParameters? translation, ScreenshakeParameters? rotation,
        string key, float? cooldown = null)
        => Screenshake(uid, translation, rotation, key,
            cooldown is not null ? TimeSpan.FromSeconds(cooldown.Value) : null);

    public void Screenshake(EntityUid uid, ScreenshakeParameters? translation, ScreenshakeParameters? rotation, string key, TimeSpan? cooldown = null)
    {
        if(!_shakeCooldowns.ContainsKey(uid)) _shakeCooldowns.Add(uid, []);
        if (_shakeCooldowns[uid].TryGetValue(key, out var time))
        {
            if (_timing.CurTime < time) return;
            _shakeCooldowns[uid].Remove(key);
        }
        if(cooldown is not null)
            _shakeCooldowns[uid].Add(key, _timing.CurTime + cooldown.Value);

        Screenshake(uid, translation, rotation);
    }

    public void Screenshake(EntityUid uid, ScreenshakeParameters? translation, ScreenshakeParameters? rotation)
    {
        if (!HasComp<EyeComponent>(uid)) return;

        var comp = EnsureComp<ScreenshakeComponent>(uid);
        var time = _timing.CurTime;
        var end = CalculateEndTimeForCommand((uid, comp), translation, rotation, time);
        var cmd = new ScreenshakeCommand(translation, rotation, time, end);

        comp.Commands.Add(cmd);
        Dirty(uid, comp);
    }

    public void Screenshake(Filter filter, ScreenshakeParameters? translation, ScreenshakeParameters? rotation,
        string key, float? cooldown = null)
        => Screenshake(filter, translation, rotation, key,
            cooldown is not null ? TimeSpan.FromSeconds(cooldown.Value) : null);

    public void Screenshake(Filter filter, ScreenshakeParameters? translation, ScreenshakeParameters? rotation, string key, TimeSpan? cooldown = null)
    {
        foreach (var player in filter.Recipients)
            if (player.AttachedEntity is { } uid)
                Screenshake(uid, translation, rotation, key, cooldown);
    }

    public void Screenshake(Filter filter, ScreenshakeParameters? translation, ScreenshakeParameters? rotation)
    {
        foreach (var player in filter.Recipients)
            if (player.AttachedEntity is { } uid)
                Screenshake(uid, translation, rotation);
    }
    #endregion
}
