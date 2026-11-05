using System.Numerics;
using Content.Shared.ADT.Explosion;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Explosion;

public sealed class ADTScreenShakeSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedContentEyeSystem _eye = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float IconSize = 32f;
    private const float TilesPerSecond = 0.7f;
    private const float Decisecond = 0.1f;
    private const float ReturnTime = 3f;

    private float _intensity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTScreenShakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeNetworkEvent<ADTScreenShakeEvent>(OnShakeEvent);
        Subs.CVar(_config, CCVars.ScreenShakeIntensity, value => _intensity = value, true);
    }

    private void OnShakeEvent(ADTScreenShakeEvent ev)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var coords = _transform.GetMapCoordinates(local);
        if (coords.MapId != ev.Center.MapId || (coords.Position - ev.Center.Position).Length() > ev.Range)
            return;

        Shake(local, ev.Duration, ev.Strength);
    }

    private void OnGetEyeOffset(Entity<ADTScreenShakeComponent> ent, ref GetEyeOffsetEvent args)
    {
        args.Offset += ent.Comp.Current;
    }

    public void Shake(EntityUid uid, float durationDeciseconds, float strength)
    {
        if (_intensity <= 0f || durationDeciseconds < 1f || strength <= 0f)
            return;

        strength *= _intensity;

        var comp = EnsureComp<ADTScreenShakeComponent>(uid);
        var now = _timing.CurTime;
        var max = (int) MathF.Round(strength * IconSize);
        var timeScalar = 1f / IconSize * TilesPerSecond;

        var last = comp.Current * IconSize;
        comp.Keys.Clear();
        comp.Keys.Add((now, comp.Current));

        var spent = 0f;
        var time = now;
        while (spent < durationDeciseconds)
        {
            var target = new Vector2(_random.Next(-max, max + 1), _random.Next(-max, max + 1));
            var step = MathF.Round(MathF.Max(MathF.Min(MathF.Abs(last.X - target.X), MathF.Abs(last.Y - target.Y)) * timeScalar, 1f));

            time += TimeSpan.FromSeconds(step * Decisecond);
            comp.Keys.Add((time, target / IconSize));

            last = target;
            spent += step;
        }

        comp.Keys.Add((time + TimeSpan.FromSeconds(ReturnTime * Decisecond), Vector2.Zero));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTScreenShakeComponent, EyeComponent>();
        while (query.MoveNext(out var uid, out var shake, out var eye))
        {
            if (shake.Keys.Count == 0 || now >= shake.Keys[^1].Time)
            {
                shake.Current = Vector2.Zero;
                RemCompDeferred<ADTScreenShakeComponent>(uid);
                _eye.UpdateEyeOffset((uid, eye));
                continue;
            }

            shake.Current = Sample(shake.Keys, now);
            _eye.UpdateEyeOffset((uid, eye));
        }
    }

    private static Vector2 Sample(List<(TimeSpan Time, Vector2 Offset)> keys, TimeSpan now)
    {
        for (var i = 1; i < keys.Count; i++)
        {
            var (time, offset) = keys[i];
            if (now > time)
                continue;

            var (prevTime, prevOffset) = keys[i - 1];
            var length = (time - prevTime).TotalSeconds;
            if (length <= 0)
                return offset;

            var t = (float) ((now - prevTime).TotalSeconds / length);
            return Vector2.Lerp(prevOffset, offset, t);
        }

        return keys[^1].Offset;
    }
}
