using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.FiringPin;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.ADT.Weapons.Ranged.Systems;
using Content.Shared.Atmos;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Weapons.Ranged;

public sealed partial class GunHeatSystem : SharedGunHeatSystem
{
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    private const float SyncStep = 2f;

    private static readonly TimeSpan _passiveCoolingInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextPassiveCooling;

    private bool _firing;

    private static readonly TimeSpan _syncHold = TimeSpan.FromSeconds(2);

    private readonly Dictionary<EntityUid, TimeSpan> _lastShot = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GunHeatComponent> ent, ref MapInitEvent _)
    {
        var temperature = EnsureComp<TemperatureComponent>(ent);
        if (ent.Comp.CoolingTime <= 0f)
            EnsureComp<AtmosExposedComponent>(ent);

        _temperature.EnsureGunHeatCapacity((ent, temperature), ent.Comp.HeatPerShot);
        if (ent.Comp.TemperaturePerShot <= 0f)
        {
            ent.Comp.TemperaturePerShot = temperature.HeatCapacity > 0f
                ? ent.Comp.HeatPerShot / temperature.HeatCapacity
                : 0f;
        }

        Sync(ent, temperature.Temperature, force: true);
    }

    protected override void AddShotHeat(Entity<GunHeatComponent> ent, int shots, float rate)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temperature))
            return;

        _lastShot[ent] = Timing.CurTime;

        _firing = true;
        try
        {
            var joules = ent.Comp.TemperaturePerShot > 0f
                ? ent.Comp.TemperaturePerShot * shots * rate * temperature.HeatCapacity
                : ent.Comp.HeatPerShot * shots * rate;
            _temperature.ChangeHeat((ent, temperature), joules, ignoreHeatResistance: true);
        }
        finally
        {
            _firing = false;
        }
    }

    [SubscribeLocalEvent]
    private void OnTemperatureAttempt(Entity<GunHeatComponent> ent, ref TemperatureChangeAttemptEvent args)
    {
        if (_firing || args.TemperatureDelta <= 0f)
            return;

        var limit = ent.Comp.JamTemperature - ent.Comp.EnvironmentHeatMargin;
        if (args.CurrentTemperature + args.TemperatureDelta > limit)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GunHeatComponent> ent, ref ComponentShutdown args)
        => _lastShot.Remove(ent);

    [SubscribeLocalEvent]
    private void OnTemperatureChange(Entity<GunHeatComponent> ent, ref TemperatureChangedEvent args)
    {
        Sync(ent, args.CurrentTemperature);

        if (_firing && ent.Comp.FailureTemperature <= 0f && args.CurrentTemperature >= ent.Comp.MeltTemperature)
            MeltFiringPin(ent);
    }

    private void Sync(Entity<GunHeatComponent> ent, float temperature, bool force = false)
    {
        if (!force && MathF.Abs(temperature - ent.Comp.Temperature) < SyncStep)
            return;

        if (!force && _lastShot.TryGetValue(ent, out var lastShot) && Timing.CurTime < lastShot + _syncHold)
            return;

        ent.Comp.Temperature = temperature;
        Dirty(ent);
    }

    private void MeltFiringPin(Entity<GunHeatComponent> ent)
    {
        if (!TryComp<FiringPinHolderComponent>(ent, out var holder)
            || !_container.TryGetContainer(ent, holder.ContainerId, out var container)
            || container.ContainedEntities.Count == 0)
            return;

        foreach (var pin in container.ContainedEntities.ToArray())
        {
            if (ent.Comp.MeltedPin is { } melted)
                SpawnNextToOrDrop(melted, ent);

            Audio.PlayPvs(ent.Comp.MeltSound, ent);
            Popup.PopupEntity(Loc.GetString("gun-heat-pin-melted", ("gun", ent.Owner)), ent, PopupType.MediumCaution);

            QueueDel(pin);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Timing.CurTime < _nextPassiveCooling)
            return;

        _nextPassiveCooling = Timing.CurTime + _passiveCoolingInterval;

        // The air cools guns through the temperature system; this keeps them from staying hot forever in space.
        var query = EntityQueryEnumerator<GunHeatComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var heat, out var temperature))
        {
            var excess = temperature.Temperature - Atmospherics.T20C;
            if (excess <= 0.5f)
                continue;

            var seconds = (float) _passiveCoolingInterval.TotalSeconds;
            var cool = MathF.Max(heat.CoolRate, 0f);
            float heatLoss;
            if (heat.CoolingTime > 0f)
            {
                var tau = cool > 0f ? heat.CoolingTime / cool : float.PositiveInfinity;
                var cooled = excess * MathF.Exp(-seconds / tau);
                heatLoss = (excess - cooled) * temperature.HeatCapacity;
            }
            else
            {
                heatLoss = MathF.Min(excess * heat.PassiveCooling * cool * seconds, excess * temperature.HeatCapacity);
            }

            _temperature.ChangeHeat((uid, temperature), -heatLoss, ignoreHeatResistance: true);
        }
    }
}
