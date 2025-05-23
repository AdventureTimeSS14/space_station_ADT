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

    private float _accumulator = 0f;
    private const float UpdateInterval = 1.0f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        _accumulator -= UpdateInterval;

        var query = EntityQueryEnumerator<RTGHeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var heatComp, out var xform))
        {
            if (!_atmosphere.TryGetTileMixture(uid, xform, true, out var atmosphere))
                continue;

            if (atmosphere.Temperature >= heatComp.MaxTemperature)
                continue;

            atmosphere.Temperature = MathF.Min(
                atmosphere.Temperature + heatComp.HeatPerSecond * UpdateInterval,
                heatComp.MaxTemperature);
        }
    }
}