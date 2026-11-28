using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Sound;
using Content.Shared.ADT.IPC;
using Content.Shared.Atmos.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Sound.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.IPC;

public sealed class SiliconSystem : SharedSiliconSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedJetpackSystem _jetpack = default!;
    [Dependency] private SleepingSystem _sleeping = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EmitSoundSystem _emitSound = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private const float OverheatCheckInterval = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SiliconComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    public bool TryGetSiliconBattery(EntityUid uid, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        battery = null;
        return HasComp<SiliconComponent>(uid) && _powerCell.TryGetBatteryFromSlot(uid, out battery);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SiliconComponent>();
        while (query.MoveNext(out var uid, out var silicon))
        {
            if (curTime < silicon.NextUpdate)
                continue;

            silicon.NextUpdate = curTime + silicon.UpdateInterval;

            if (!silicon.BatteryPowered || _mobState.IsDead(uid))
                continue;

            if (!TryGetSiliconBattery(uid, out var battery))
            {
                SetChargeState((uid, silicon), 0);
                continue;
            }

            if (TryComp<MindContainerComponent>(uid, out var mindContainer) && !mindContainer.HasMind)
                continue;

            var interval = (float) silicon.UpdateInterval.TotalSeconds;
            var baseDrain = silicon.DrainPerSecond;
            var extraDrain = (GetHeatMultiplier((uid, silicon), interval) - 1f) * baseDrain + GetMovementDrain((uid, silicon));
            var drain = baseDrain + Math.Clamp(extraDrain, baseDrain * -0.9f, battery.Value.Comp.MaxCharge / 240f);

            _powerCell.TryUseCharge(uid, drain * interval);

            var chargeState = (short) MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);
            SetChargeState((uid, silicon), chargeState);
        }
    }

    private void SetChargeState(Entity<SiliconComponent> ent, short chargeState)
    {
        if (ent.Comp.ChargeState != chargeState)
        {
            ent.Comp.ChargeState = chargeState;
            Dirty(ent);
            MoveMod.RefreshMovementSpeedModifiers(ent.Owner);
        }

        SetDrained(ent, chargeState == 0);
    }

    private void SetDrained(Entity<SiliconComponent> ent, bool drained)
    {
        if (ent.Comp.Drained == drained)
            return;

        ent.Comp.Drained = drained;
        Dirty(ent);

        if (drained)
        {
            if (ent.Comp.DownOnDrained)
                _statusEffects.TryUpdateStatusEffectDuration(ent, SleepingSystem.StatusEffectForcedSleeping);

            StartDrainedSound(ent);
            return;
        }

        if (ent.Comp.DownOnDrained)
        {
            _statusEffects.TryRemoveStatusEffect(ent, SleepingSystem.StatusEffectForcedSleeping);
            _sleeping.TryWaking(ent.Owner, true);
        }

        StopDrainedSound(ent);
    }

    private void StartDrainedSound(Entity<SiliconComponent> ent)
    {
        if (ent.Comp.DrainedSound == null)
            return;

        var spam = EnsureComp<SpamEmitSoundComponent>(ent);
        spam.Sound = ent.Comp.DrainedSound;
        spam.MinInterval = ent.Comp.DrainedSoundMinInterval;
        spam.MaxInterval = ent.Comp.DrainedSoundMaxInterval;
        spam.PopUp = ent.Comp.DrainedPopup;
        Dirty(ent, spam);
        _emitSound.SetEnabled((ent.Owner, spam), true);
    }

    private void StopDrainedSound(Entity<SiliconComponent> ent)
    {
        if (ent.Comp.DrainedSound != null)
            RemComp<SpamEmitSoundComponent>(ent);
    }

    protected override void OnMobStateChanged(Entity<SiliconComponent> ent, ref MobStateChangedEvent args)
    {
        base.OnMobStateChanged(ent, ref args);

        if (args.NewMobState == MobState.Dead)
        {
            StopDrainedSound(ent);
            return;
        }

        if (args.OldMobState == MobState.Dead && args.NewMobState == MobState.Alive)
            OfferReturnToBody(ent);
    }

    private void OfferReturnToBody(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out _, out var mind)
            || mind.CurrentEntity == uid
            || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }

    private float GetHeatMultiplier(Entity<SiliconComponent> ent, float interval)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temperature)
            || !TryComp<ThermalRegulatorComponent>(ent, out var regulator)
            || !TryComp<TemperatureDamageComponent>(ent, out var temperatureDamage))
            return 1f;

        var normal = regulator.NormalBodyTemperature;
        var upperThreshold = normal + regulator.ThermalRegulationTemperatureThreshold;
        var hotThreshold = normal + regulator.ThermalRegulationTemperatureThreshold * 0.5f;
        var temp = temperature.Temperature;

        if (temp < normal)
            return 0.5f + temp / normal * 0.5f;

        if (temp <= hotThreshold)
            return 1f;

        var multiplier = Math.Min(temp / hotThreshold, 4f);

        ent.Comp.OverheatAccumulator += interval;
        if (ent.Comp.OverheatAccumulator < OverheatCheckInterval)
            return multiplier;

        ent.Comp.OverheatAccumulator -= OverheatCheckInterval;

        if (temp <= temperatureDamage.HeatDamageThreshold
            || !TryComp<FlammableComponent>(ent, out var flammable)
            || flammable.OnFire)
            return multiplier;

        _popup.PopupEntity(Loc.GetString("silicon-overheating"), ent, ent, PopupType.MediumCaution);

        if (!_random.Prob(Math.Clamp(temp / (upperThreshold * 5), 0.001f, 0.9f)))
            return multiplier;

        _flammable.AdjustFireStacks(ent, Math.Clamp(ent.Comp.FireStackMultiplier, -10, 10), flammable);
        _flammable.Ignite(ent, ent, flammable);
        return multiplier;
    }

    private float GetMovementDrain(Entity<SiliconComponent> ent)
    {
        if (!TryComp<MovementSpeedModifierComponent>(ent, out var movement)
            || !TryComp<PhysicsComponent>(ent, out var physics)
            || !TryComp<InputMoverComponent>(ent, out var input))
            return 0f;

        var idleReduction = -ent.Comp.DrainPerSecond * ent.Comp.IdleDrainReduction;

        if (input.HeldMoveButtons == MoveButtons.None || _jetpack.IsUserFlying(ent) || movement.CurrentSprintSpeed <= 0f)
            return idleReduction;

        var velocity = physics.LinearVelocity;
        var speedFraction = velocity.Length() / movement.CurrentSprintSpeed;
        return Math.Clamp(ent.Comp.DrainPerSecond * (speedFraction - 1f), idleReduction, 0f);
    }
}
