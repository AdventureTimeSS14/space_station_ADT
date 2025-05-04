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

    /// <summary>
    /// Shoot lightning bolts depending on accumulated power, but only once per interval.
    /// </summary>
    public void SupermatterZap(EntityUid uid, SupermatterComponent sm, float frameTime)
    {
        if (!sm.HasBeenPowered)
        return;
        
        while (_zapAccumulator >= sm.ZapTimer)
        {
            _zapAccumulator -= sm.ZapTimer;

            var power = sm.Power;
            var integrity = GetIntegrity(sm);
            var zapRange = Math.Clamp(power / 1000, 2, 7);

            int zapCount = 1 + (int)(power / 2000);

            if (_random.Prob(0.2f))
                zapCount++;

            if (integrity < 50)
                zapCount++;
                sm.ZapTimer -= TimeSpan.FromSeconds(30);

            zapCount = Math.Clamp(zapCount, 1, 5);

            int zapPower = 0;
            if (power >= _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold))
                zapPower++;
                sm.ZapTimer -= TimeSpan.FromSeconds(10);
            if (power >= _config.GetCVar(ADTCCVars.SupermatterCriticalPowerPenaltyThreshold))
                zapPower++;
                sm.ZapTimer -= TimeSpan.FromSeconds(5);

            zapPower = Math.Clamp(zapPower, 1, 3);

            _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower]);
        }
    }
}
