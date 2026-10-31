using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class BarotraumaSystem
    {
        public void RefreshPressureResistance(EntityUid uid)
        {
            if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
                UpdateCachedResistances(uid, barotrauma);
        }
    }
}
