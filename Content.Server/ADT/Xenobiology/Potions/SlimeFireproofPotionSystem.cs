using Content.Server.Popups;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology.Potions;

public sealed partial class SlimeFireproofPotionSystem : EntitySystem
{
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private FireProtectionSystem _fireProtection = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SlimeSpeedPotionSystem _speedPotion = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextCheck;
    private readonly List<EntityUid> _expired = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeFireproofPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        _expired.Clear();
        var fireproof = EntityQueryEnumerator<SlimeFireproofPotionAppliedComponent>();
        while (fireproof.MoveNext(out var uid, out var applied))
        {
            if (_timing.CurTime >= applied.EndTime)
                _expired.Add(uid);
        }

        foreach (var uid in _expired)
        {
            if (TryComp<SlimeFireproofPotionAppliedComponent>(uid, out var applied))
                RestoreFireproof((uid, applied));
        }

        _expired.Clear();
        var speed = EntityQueryEnumerator<SlimeSpeedPotionAppliedComponent>();
        while (speed.MoveNext(out var uid, out var applied))
        {
            if (_timing.CurTime >= applied.EndTime)
                _expired.Add(uid);
        }

        foreach (var uid in _expired)
        {
            if (TryComp<SlimeSpeedPotionAppliedComponent>(uid, out var applied))
                _speedPotion.RestoreSpeed((uid, applied));
        }
    }

    private void OnAfterInteract(Entity<SlimeFireproofPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;

        if (!TryComp<SlimeFireproofPotionAppliedComponent>(target, out var applied))
        {
            applied = AddComp<SlimeFireproofPotionAppliedComponent>(target);

            if (TryComp<TemperatureProtectionComponent>(target, out var oldTemperature))
            {
                applied.HadTemperatureProtection = true;
                applied.OriginalHeatCoefficient = oldTemperature.Coefficient;
            }

            if (TryComp<FireProtectionComponent>(target, out var oldFire))
            {
                applied.HadFireProtection = true;
                applied.OriginalFireReduction = oldFire.Reduction;
            }
        }

        applied.EndTime = _timing.CurTime + ent.Comp.Duration;

        var temperatureProtection = EnsureComp<TemperatureProtectionComponent>(target);
        _temperature.SetHeatProtection((target, temperatureProtection), 0f);

        var fireProtection = EnsureComp<FireProtectionComponent>(target);
        _fireProtection.SetFireProtection(fireProtection, 1f);

        ent.Comp.RemainingUses -= 1;
        _popup.PopupEntity(Loc.GetString("xeno-potion-fireproof-applied",
            ("uses", ent.Comp.RemainingUses),
            ("minutes", (int) ent.Comp.Duration.TotalMinutes)), args.User, args.User);

        if (ent.Comp.RemainingUses <= 0)
            PredictedQueueDel(args.Used);
    }

    private void RestoreFireproof(Entity<SlimeFireproofPotionAppliedComponent> ent)
    {
        if (TryComp<TemperatureProtectionComponent>(ent, out var temperature))
        {
            if (ent.Comp.HadTemperatureProtection)
                _temperature.SetHeatProtection((ent.Owner, temperature), ent.Comp.OriginalHeatCoefficient);
            else
                RemComp<TemperatureProtectionComponent>(ent);
        }

        if (TryComp<FireProtectionComponent>(ent, out var fire))
        {
            if (ent.Comp.HadFireProtection)
                _fireProtection.SetFireProtection(fire, ent.Comp.OriginalFireReduction);
            else
                RemComp<FireProtectionComponent>(ent);
        }

        RemComp<SlimeFireproofPotionAppliedComponent>(ent);
        _popup.PopupEntity(Loc.GetString("xeno-potion-fireproof-expired", ("target", ent.Owner)), ent);
    }
}
