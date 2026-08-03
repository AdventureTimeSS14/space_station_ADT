using Content.Shared.ADT.Camera; // ADT screenshake
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    [Dependency] private readonly ScreenshakeSystem _screenshake = default!; // ADT screenshake
    [Dependency] private readonly SharedGameTicker _ticker = default!; // ADT screenshake
    protected const float GravityKick = 100.0f;
    protected const float ShakeCooldown = 0.2f;

    private void UpdateShake()
    {
        var curTime = Timing.CurTime;
        var gravityQuery = GetEntityQuery<GravityComponent>();
        var query = EntityQueryEnumerator<GravityShakeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextShake <= curTime)
            {
                if (comp.ShakeTimes == 0 || !gravityQuery.TryGetComponent(uid, out var gravity))
                {
                    RemCompDeferred<GravityShakeComponent>(uid);
                    continue;
                }

                ShakeGrid(uid, gravity);
                comp.ShakeTimes--;
                comp.NextShake += TimeSpan.FromSeconds(ShakeCooldown);
                Dirty(uid, comp);
            }
        }
    }

    public void StartGridShake(EntityUid uid, GravityComponent? gravity = null)
    {
        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref gravity, false))
            return;

        if (Timing.CurTime - _ticker.RoundStartTimeSpan < TimeSpan.FromSeconds(10)) // ADT screenshake
            return;

        var shake = new ScreenshakeParameters { Trauma = 0.8f, DecayRate = 0.04f, Frequency = 0.015f };
        _screenshake.Screenshake(Filter.BroadcastGrid(uid), shake, null); // ADT screenshake

        if (!TryComp<GravityShakeComponent>(uid, out var shakeComp))
        {
            shakeComp = AddComp<GravityShakeComponent>(uid);
            shakeComp.NextShake = Timing.CurTime;
        }

        shakeComp.ShakeTimes = 10;
        Dirty(uid, shakeComp);
    }

    protected virtual void ShakeGrid(EntityUid uid, GravityComponent? comp = null) {}
}
