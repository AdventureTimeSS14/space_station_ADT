using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Lightning;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Supermatter.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.ADT.Supermatter.Systems;

public sealed partial class SupermatterSystem
{
    private TimeSpan _zapAccumulator = TimeSpan.Zero;
    private static readonly TimeSpan ZapInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Shoot lightning bolts depending on accumulated power, but only once per interval.
    /// </summary>
    public void SupermatterZap(EntityUid uid, SupermatterComponent sm, float frameTime)
    {
        _zapAccumulator += TimeSpan.FromSeconds(frameTime);

        if (_zapAccumulator < ZapInterval)
            return;

        int zapTimes = (int)(_zapAccumulator.Ticks / ZapInterval.Ticks);
        _zapAccumulator -= TimeSpan.FromTicks(ZapInterval.Ticks * zapTimes);

        var power = sm.Power;
        var integrity = GetIntegrity(sm);
        var zapRange = Math.Clamp(power / 1000, 2, 7);

        for (int i = 0; i < zapTimes; i++)
        {
            int zapPower = 0;
            int zapCount = 0;

            if (_random.Prob(0.05f))
                zapCount++;

            if (power >= _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold))
                zapCount += 2;

            if (power >= _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold))
            {
                zapPower++;
                zapCount++;
            }

            if (power >= _config.GetCVar(ADTCCVars.SupermatterCriticalPowerPenaltyThreshold))
            {
                zapPower++;
                zapCount++;
            }

            if (integrity < 25)
                zapCount++;

            if (power >= _config.GetCVar(ADTCCVars.SupermatterMinPowerToLighting))
                zapCount = Math.Max(zapCount, 1);

            _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower]);
        }
    }
}