using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Body.Pulse;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Medical.Stethoscope;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server.ADT.Body;

public sealed class HeartPulseSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const string HeartCategory = "Heart";
    private const float ThreadyBloodLevel = 0.85f;
    private const float NoPulseBloodLevel = 0.3f;
    private const int MinRestingBpm = 65;
    private const int RestingBpmSpread = 21;
    private const float BloodLossBpm = 130f;
    private const int BruisedHeartBpm = 10;
    private const int DamagedHeartBpm = 20;
    private const int SoftCriticalBpm = 15;
    private const float CriticalBpmFactor = 0.65f;
    private const int MaxBpm = 180;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, ADTStethoscopeListenedEvent>(OnStethoscopeListened);
    }

    private void OnStethoscopeListened(Entity<MobStateComponent> ent, ref ADTStethoscopeListenedEvent args)
    {
        if (GetPulse(ent) is not { } pulse)
            return;

        _popup.PopupEntity(pulse.Describe(), args.User, args.User, pulse.Normal ? PopupType.Small : PopupType.SmallCaution);
    }

    public PulseReading? GetPulse(EntityUid body)
    {
        var hasHeart = _organHealth.TryGetOrgan(body, HeartCategory, out var heart);
        TryComp<OrganFunctionsComponent>(body, out var functions);

        if (!hasHeart && functions?.ExpectedCategories.Contains(HeartCategory) != true)
            return null;

        var blood = TryComp<BloodstreamComponent>(body, out var bloodstream)
            ? _bloodstream.GetBloodLevel((body, bloodstream))
            : 1f;

        if (!hasHeart || functions?.HeartAttackStart != null || _mobState.IsDead(body) || blood <= NoPulseBloodLevel)
            return new PulseReading(PulseStrength.Absent, PulsePressure.Thready, 0);

        var strength = heart.Comp.Stage switch
        {
            OrganStage.Healthy => PulseStrength.Regular,
            OrganStage.Bruised => PulseStrength.Weak,
            _ => PulseStrength.Irregular,
        };

        return new PulseReading(strength, blood <= ThreadyBloodLevel ? PulsePressure.Thready : PulsePressure.Strong, GetBpm(body, heart.Comp.Stage, blood));
    }

    private int GetBpm(EntityUid body, OrganStage heartStage, float blood)
    {
        var bpm = MinRestingBpm + body.Id % RestingBpmSpread + Math.Max(0f, 1f - blood) * BloodLossBpm;

        bpm += heartStage switch
        {
            OrganStage.Healthy => 0,
            OrganStage.Bruised => BruisedHeartBpm,
            _ => DamagedHeartBpm,
        };

        if (TryComp<MobStateComponent>(body, out var mobState))
        {
            if (mobState.CurrentState == MobState.SoftCritical)
                bpm += SoftCriticalBpm;
            else if (mobState.CurrentState == MobState.Critical)
                bpm *= CriticalBpmFactor;
        }

        return Math.Min((int) bpm, MaxBpm);
    }
}
