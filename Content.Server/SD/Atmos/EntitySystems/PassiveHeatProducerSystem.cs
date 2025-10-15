using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

public sealed class PassiveHeatProducerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PassiveHeatProducerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PassiveHeatProducerComponent>();
        var currentTime = _gameTiming.CurTime;
        var toHeat = new List<(GasMixture mixture, float energy)>();

        // Сначала собираем все данные
        while (query.MoveNext(out var uid, out var heater))
        {
            if (!_power.IsPowered(uid) || heater.NextUpdate > currentTime)
                continue;

            heater.NextUpdate = currentTime + TimeSpan.FromSeconds(1);

            var mixture = _atmosphere.GetContainingMixture(uid, excite: true);
            if (mixture != null)
            {
                toHeat.Add((mixture, heater.EnergyPerSecond));
            }
        }
        foreach (var (mixture, energy) in toHeat)
        {
            AddHeatToMixture(mixture, energy);
        }
    }

    private void OnMapInit(EntityUid uid, PassiveHeatProducerComponent component, MapInitEvent args)
    {
        component.NextUpdate = _gameTiming.CurTime;
    }

    private void AddHeatToMixture(GasMixture mixture, float energy)
    {
        if (mixture.TotalMoles > 0)
        {
            var heatCapacity = _atmosphere.GetHeatCapacity(mixture, true);
            if (heatCapacity > 0)
            {
                var maxTempIncrease = 50f;
                var tempIncrease = energy / heatCapacity;

                if (tempIncrease > maxTempIncrease)
                    tempIncrease = maxTempIncrease;

                mixture.Temperature += tempIncrease;
            }
        }
    }
}
