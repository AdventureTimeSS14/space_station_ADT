using Content.Shared.ADT.Supermatter.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Supermatter.Processing.Systems;

public sealed partial class SupermatterLightingSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;

    /// <summary>
    /// Shoot lightning bolts depending on accumulated power.
    /// </summary>
    public void SupermatterZap(EntityUid uid, SupermatterComponent sm)
    {
        var zapPower = 0;
        var zapCount = 0;
        var zapRange = Math.Clamp(sm.Power / 1000, 2, 7);

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

        if (zapCount >= 1)
            _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower]);
    }
}