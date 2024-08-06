// Simple Station

using Content.Server.Power.Components;
using Content.Shared.ADT.Silicon.Systems;
// using Content.Server.Bed.Sleep; - nixsilvam: ну предположим это не нужно вообще теперь
using Content.Shared.Bed.Sleep;
// using Content.Server.Sound.Components; - nixsilvam: это в целом не используется тут, хз зачем оно было
using Content.Server.ADT.Silicon.Charge;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.Silicon.Death;

public sealed class SiliconDeathSystem : EntitySystem
{
    [Dependency] private readonly SleepingSystem _sleep = default!;
    [Dependency] private readonly SiliconChargeSystem _silicon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconDownOnDeadComponent, SiliconChargeStateUpdateEvent>(OnSiliconChargeStateUpdate);
    }

    private void OnSiliconChargeStateUpdate(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, SiliconChargeStateUpdateEvent args)
    {
        _silicon.TryGetSiliconBattery(uid, out var batteryComp, out var batteryUid);

        if (args.ChargeState == ChargeState.Dead && siliconDeadComp.Dead)
        {
            siliconDeadComp.WakeToken?.Cancel();
            return;
        }

        if (args.ChargeState == ChargeState.Dead && !siliconDeadComp.Dead)
            SiliconDead(uid, siliconDeadComp, batteryComp, batteryUid);
        else if (args.ChargeState != ChargeState.Dead && siliconDeadComp.Dead)
        {
            if (siliconDeadComp.DeadBuffer > 0)
            {
                siliconDeadComp.WakeToken?.Cancel(); // This should never matter, but better safe than loose timers.

                var wakeToken = new CancellationTokenSource();
                siliconDeadComp.WakeToken = wakeToken;

                // If battery is dead, wait the dead buffer time and then wake it up.
                Timer.Spawn(TimeSpan.FromSeconds(siliconDeadComp.DeadBuffer), () =>
                {
                    if (wakeToken.IsCancellationRequested)
                        return;

                    SiliconUnDead(uid, siliconDeadComp, batteryComp, batteryUid);
                }, wakeToken.Token);
            }
            else
                SiliconUnDead(uid, siliconDeadComp, batteryComp, batteryUid);
        }
    }

    private void SiliconDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        var deadEvent = new SiliconChargeDyingEvent(uid, batteryComp, batteryUid);
        RaiseLocalEvent(uid, deadEvent);

        if (deadEvent.Cancelled)
            return;

        EntityManager.EnsureComponent<SleepingComponent>(uid);
        EntityManager.EnsureComponent<ForcedSleepingComponent>(uid);

        siliconDeadComp.Dead = true;

        RaiseLocalEvent(uid, new SiliconChargeDeathEvent(uid, batteryComp, batteryUid));
    }

    private void SiliconUnDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        _sleep.TryWaking(uid, true, null);

        siliconDeadComp.Dead = false;

        RaiseLocalEvent(uid, new SiliconChargeAliveEvent(uid, batteryComp, batteryUid));
    }
}

/// <summary>
///     A cancellable event raised when a Silicon is about to go down due to charge.
/// </summary>
/// <remarks>
///     This probably shouldn't be modified unless you intend to fill the Silicon's battery,
///     as otherwise it'll just be triggered again next frame.
/// </remarks>
public sealed class SiliconChargeDyingEvent : CancellableEntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeDyingEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}

/// <summary>
///     An event raised after a Silicon has gone down due to charge.
/// </summary>
public sealed class SiliconChargeDeathEvent : EntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeDeathEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}

/// <summary>
///     An event raised after a Silicon has reawoken due to an increase in charge.
/// </summary>
public sealed class SiliconChargeAliveEvent : EntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeAliveEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}
