using Content.Server.Anomaly.Components;

namespace Content.Server.Anomaly;

public sealed partial class AnomalySystem
{
    public void SetPointMultiplier(EntityUid uid, float multiplier, AnomalyVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.PointMultiplier = multiplier;
    }
}
