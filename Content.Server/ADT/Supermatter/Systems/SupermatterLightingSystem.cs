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
    /// Shoot lightning bolts depending on accumulated power.
    /// </summary>
    public void SupermatterZap(EntityUid uid, SupermatterComponent sm)
    {
        var zapPower = 0;
        var zapCount = 0;
        var zapRange = Math.Clamp(sm.Power / 1000, 2, 7);

        var integrity = GetIntegrity(sm);


        if (_random.Prob(0.05f))
            zapCount += 1;

        if (sm.Power >= _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold))
            zapCount += 2;

        if (sm.Power >= _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold))
        {
            zapPower += 1;
            zapCount += 1;
        }

        if (sm.Power >= _config.GetCVar(ADTCCVars.SupermatterCriticalPowerPenaltyThreshold))
        {
            zapPower += 1;
            zapCount += 1;
        }

        if (integrity < 25)
        {
            zapCount += 1;
        }

        if (sm.Power >= _config.GetCVar(ADTCCVars.SupermatterMinPowerToLighting))
            zapCount = Math.Max(zapCount, 1);


        _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower]);
    }
}
