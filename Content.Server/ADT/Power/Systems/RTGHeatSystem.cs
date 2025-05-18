using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Server.Temperature.Components;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Content.Shared.ADT.Power.Components;

namespace Content.Server.ADT.Power.Systems;

/// <summary>
/// Система, нагревающая газ в окружающем тайле.
/// </summary>
public sealed class RTGHeatSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RTGHeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var heatComp, out var xform))
        {
            heatComp.TimeSinceLastHeat += frameTime;

            if (heatComp.TimeSinceLastHeat < heatComp.HeatInterval)
                continue;

            heatComp.TimeSinceLastHeat = 0f;

            var atmosphere = _atmosphere.GetTileMixture(uid, true);

            if (atmosphere == null)
                continue;

            if (atmosphere.Temperature >= heatComp.MaxTemperature)
                continue;

            var newTemp = MathF.Min(atmosphere.Temperature + heatComp.HeatPerSecond * heatComp.HeatInterval, heatComp.MaxTemperature);
            atmosphere.Temperature = newTemp;
        }
    }
}