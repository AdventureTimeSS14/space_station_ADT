// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Server.Atmos.EntitySystems;
using Content.Shared._RMC14.Atmos;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Temperature;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Atmos;

public sealed class RMCFlammableSystem : SharedRMCFlammableSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    private const float ThermalProtectionWeight = 0.5f;

    public override float ApplyThermalProtection(EntityUid uid, float fireMultiplier)
    {
        var ev = new ModifyChangedTemperatureEvent(1f);
        RaiseLocalEvent(uid, ev);

        var heating = Math.Clamp(ev.TemperatureDelta, 0f, 1f);
        var thermalMultiplier = 1f - (1f - heating) * ThermalProtectionWeight;

        return Math.Min(fireMultiplier, thermalMultiplier);
    }

    protected override bool HasOxygen(EntityUid uid)
    {
        var air = _atmosphere.GetContainingMixture(uid);
        return air != null && air.GetMoles(Gas.Oxygen) >= 1f;
    }

    public override bool Ignite(Entity<FlammableComponent?> flammable, int intensity, int duration, int? maxStacks, DamageSpecifier? tileDamage = null)
    {
        if (!Resolve(flammable, ref flammable.Comp, false))
            return false;

        var hadBypass = HasComp<RMCFireBypassActiveComponent>(flammable);

        var stacks = flammable.Comp.FireStacks + duration;
        if (maxStacks != null && stacks > maxStacks)
            stacks = maxStacks.Value;

        _flammable.SetFireStacks(flammable, stacks, flammable, true);
        if (!flammable.Comp.OnFire)
            return false;

        if (hadBypass)
            EnsureComp<RMCFireBypassActiveComponent>(flammable);

        var onFire = EnsureComp<OnFireComponent>(flammable);
        onFire.Intensity = intensity;
        onFire.Duration = duration;
        onFire.TileDamage = tileDamage;
        Dirty(flammable.Owner, onFire);

        return true;
    }

    public override void Extinguish(Entity<FlammableComponent?> flammable)
    {
        if (!Resolve(flammable, ref flammable.Comp, false))
            return;

        _flammable.Extinguish(flammable, flammable);
    }

    public override void Pat(Entity<FlammableComponent?> flammable, int stacks)
    {
        if (!Resolve(flammable, ref flammable.Comp, false))
            return;

        _flammable.AdjustFireStacks(flammable, stacks, flammable);
    }

    public override void AdjustStacks(Entity<FlammableComponent?> flammable, int stacks)
    {
        if (!Resolve(flammable, ref flammable.Comp, false))
            return;

        _flammable.AdjustFireStacks(flammable, stacks, flammable);
    }

    public void DoStopDropRollAnimation(EntityUid uid, TimeSpan length)
    {
        if (!HasComp<RMCStopDropRollVisualsComponent>(uid))
            return;

        if (!_actionBlocker.CanMove(uid))
            return;

        RaiseNetworkEvent(new RMCStopDropRollVisualsNetworkEvent(GetNetEntity(uid), length), Filter.Pvs(uid));
    }
}
