using Content.Server.Atmos.EntitySystems;
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
            var atmos = _atmosphere.GetContainingMixture(uid, false, true);
            if (atmos == null)
                continue;

            if (atmos.Temperature >= heatComp.MaxTemperature)
                continue;

            atmos.Temperature = MathF.Min(
                atmos.Temperature + heatComp.HeatPerSecond * UpdateInterval,
                heatComp.MaxTemperature);
        }
    }
}
