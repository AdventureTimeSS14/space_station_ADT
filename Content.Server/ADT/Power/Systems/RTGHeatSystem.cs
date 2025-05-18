using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.ADT.Power.Components;

namespace Content.Server.ADT.Power.Systems;

/// <summary>
/// Просто система нагрева атмосферы вокруг ентити.
/// Греет до указанной температуры.
/// </summary>
public sealed class RTGHeatSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RTGHeatComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeSinceLastHeat += frameTime;

            if (comp.TimeSinceLastHeat >= comp.HeatInterval)
            {
                var atmos = _atmosphere.GetContainingMixture(uid, false, true);
                if (atmos == null)
                    continue;

                if (atmos.Temperature >= comp.MaxTemperature)
                    continue;

                var delta = comp.HeatPerSecond;
                if (atmos.Temperature + delta > comp.MaxTemperature)
                    delta = comp.MaxTemperature - atmos.Temperature;

                atmos.Temperature += delta;

                comp.TimeSinceLastHeat = 0f;
            }
        }
    }
}