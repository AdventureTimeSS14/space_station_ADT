// Simple Station

using Robust.Shared.Random;
using Content.Shared.ADT.Silicon.Components;
using Content.Server.Power.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Temperature.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.Movement.Systems;
using Content.Server.Body.Components;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Silicon.Charge;

public sealed class SiliconChargeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, ComponentStartup>(OnSiliconStartup);
    }

    public bool TryGetSiliconBattery(EntityUid silicon, [NotNullWhen(true)] out BatteryComponent? batteryComp, out EntityUid batteryUid)
    {
        batteryComp = null;
        batteryUid = silicon;

        if (!EntityManager.TryGetComponent(silicon, out SiliconComponent? siliconComp))
            return false;

        /// этот блок по идее можно пропустить ибо у нас визардовские борги
        // if (siliconComp.BatteryContainer != null &&
        //     siliconComp.BatteryContainer.ContainedEntities.Count > 0 &&
        //     TryComp(siliconComp.BatteryContainer.ContainedEntities[0], out batteryComp))
        // {
        //     batteryUid = siliconComp.BatteryContainer.ContainedEntities[0];
        //     return true;
        // }

        if (TryComp(silicon, out batteryComp))
            return true;

        return false;
    }

    private void OnSiliconStartup(EntityUid uid, SiliconComponent component, ComponentStartup args)
    {
        if (component.BatterySlot == null)
            return;
    /// этот блок по идее можно пропустить ибо у нас визардовские борги
        // var container = _container.GetContainer(uid, component.BatterySlot);
        // component.BatteryContainer = container;

        if (component.EntityType.GetType() != typeof(SiliconType))
            DebugTools.Assert("SiliconComponent.EntityType is not a SiliconType enum.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // For each siliconComp entity with a battery component, drain their charge.
        var query = EntityQueryEnumerator<SiliconComponent>();
        while (query.MoveNext(out var silicon, out var siliconComp))
        {
            if (!siliconComp.BatteryPowered)
                continue;

            // Check if the Silicon is an NPC, and if so, follow the delay as specified in the CVAR.
            if (siliconComp.EntityType.Equals(SiliconType.Npc))
            {
                var updateTime = _config.GetCVar(SimpleStationCCVars.SiliconNpcUpdateTime);
                if (_timing.CurTime - siliconComp.LastDrainTime < TimeSpan.FromSeconds(updateTime))
                    continue;

                siliconComp.LastDrainTime = _timing.CurTime;
            }

            // If you can't find a battery, set the indicator and skip it.
            if (!TryGetSiliconBattery(silicon, out var batteryComp, out var battery))
            {
                UpdateChargeState(battery, ChargeState.Invalid, siliconComp);
                continue;
            }

            // If the silicon is dead, skip it.
            if (_mobState.IsDead(silicon))
                continue;

            var drainRate = siliconComp.DrainPerSecond;

            // All multipliers will be subtracted by 1, and then added together, and then multiplied by the drain rate. This is then added to the base drain rate.
            // This is to stop exponential increases, while still allowing for less-than-one multipliers.
            var drainRateFinalAddi = 0f;

            // TODO: Devise a method of adding multis where other systems can alter the drain rate.
            // Maybe use something similar to refreshmovespeedmodifiers, where it's stored in the component.
            // Maybe it doesn't matter, and stuff should just use static drain?
            if (!siliconComp.EntityType.Equals(SiliconType.Npc)) // Don't bother checking heat if it's an NPC. It's a waste of time, and it'd be delayed due to the update time.
                drainRateFinalAddi += SiliconHeatEffects(silicon, frameTime) - 1; // This will need to be changed at some point if we allow external batteries, since the heat of the Silicon might not be applicable.

            // Ensures that the drain rate is at least 10% of normal,
            // and would allow at least 4 minutes of life with a max charge, to prevent cheese.
            drainRate += Math.Clamp(drainRateFinalAddi, drainRate * -0.9f, batteryComp.MaxCharge / 240);

            // Drain the battery.
            _battery.UseCharge(battery, frameTime * drainRate, batteryComp);

            // Figure out the current state of the Silicon.
            var chargePercent = batteryComp.CurrentCharge / batteryComp.MaxCharge;

            var currentState = chargePercent switch
            {
                var x when x > siliconComp.ChargeThresholdMid => ChargeState.Full,
                var x when x > siliconComp.ChargeThresholdLow => ChargeState.Mid,
                var x when x > siliconComp.ChargeThresholdCritical => ChargeState.Low,
                var x when x > 0 || siliconComp.ChargeThresholdCritical == 0 => ChargeState.Critical,
                _ => ChargeState.Dead,
            };

            UpdateChargeState(silicon, currentState, siliconComp);
        }
    }

    /// <summary>
    ///     Checks if anything needs to be updated, and updates it.
    /// </summary>
    public void UpdateChargeState(EntityUid uid, ChargeState state, SiliconComponent component)
    {
        if (component.ChargeState == state)
            return;

        component.ChargeState = state;

        RaiseLocalEvent(uid, new SiliconChargeStateUpdateEvent(state));

        _moveMod.RefreshMovementSpeedModifiers(uid);
    }

    private float SiliconHeatEffects(EntityUid silicon, float frameTime)
    {
        if (!EntityManager.TryGetComponent<TemperatureComponent>(silicon, out var temperComp) ||
            !EntityManager.TryGetComponent<ThermalRegulatorComponent>(silicon, out var thermalComp))
        {
            return 0;
        }

        var siliconComp = EntityManager.GetComponent<SiliconComponent>(silicon);

        // If the Silicon is hot, drain the battery faster, if it's cold, drain it slower, capped.
        var upperThresh = thermalComp.NormalBodyTemperature + thermalComp.ThermalRegulationTemperatureThreshold;
        var upperThreshHalf = thermalComp.NormalBodyTemperature + thermalComp.ThermalRegulationTemperatureThreshold * 0.5f;

        // Check if the silicon is in a hot environment.
        if (temperComp.CurrentTemperature > upperThreshHalf)
        {
            // Divide the current temp by the max comfortable temp capped to 4, then add that to the multiplier.
            var hotTempMulti = Math.Min(temperComp.CurrentTemperature / upperThreshHalf, 4);

            // If the silicon is hot enough, it has a chance to catch fire.

            siliconComp.OverheatAccumulator += frameTime;
            if (siliconComp.OverheatAccumulator >= 5)
            {
                siliconComp.OverheatAccumulator -= 5;

                if (EntityManager.TryGetComponent<FlammableComponent>(silicon, out var flamComp) &&
                    temperComp.CurrentTemperature > temperComp.HeatDamageThreshold &&
                    !flamComp.OnFire &&
                    _random.Prob(Math.Clamp(temperComp.CurrentTemperature / (upperThresh * 5), 0.001f, 0.9f)))
                {
                    //_flammable.Ignite(silicon, flamComp); // починить Ignite
                }
                else if ((flamComp == null || !flamComp.OnFire) &&
                        _random.Prob(Math.Clamp(temperComp.CurrentTemperature / upperThresh, 0.001f, 0.75f)))
                {
                    _popup.PopupEntity(Loc.GetString("silicon-overheating"), silicon, silicon, PopupType.SmallCaution);
                }
            }

            return hotTempMulti;
        }

        // Check if the silicon is in a cold environment.
        if (temperComp.CurrentTemperature < thermalComp.NormalBodyTemperature)
        {
            var coldTempMulti = 0.5f + temperComp.CurrentTemperature / thermalComp.NormalBodyTemperature * 0.5f;

            return coldTempMulti;
        }

        return 0;
    }
}
