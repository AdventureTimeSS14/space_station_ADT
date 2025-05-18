using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.ADT.Power.Systems;

/// <summary>
/// Просто система нагрева атмосферы вокруг ентити.
/// </summary>
public sealed class RTGHeatSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RTGComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeSinceLastHeat += frameTime;

            if (comp.TimeSinceLastHeat >= comp.HeatInterval)
            {
                var atmos = _atmosphere.GetContainingMixture(uid, false, true);
                if (atmos == null)
                    continue;

                atmos.Temperature += comp.HeatPerSecond;

                _atmosphere.Invalidate(atmos);

                comp.TimeSinceLastHeat = 0f;
            }
        }
    }
}