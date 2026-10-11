using Content.Shared.ADT.Implants.KereznikovImplant;
using Content.Server.ADT.Implants.Sandevistan;
using Content.Shared.ADT.Implants.Sandevistan;
using Content.Shared.ADT.Implants.SpasezhnikovImplant;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Implants.SpasezhnikovImplant;

public sealed class SpasezhnikovImplantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpasezhnikovImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SpasezhnikovImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<SpasezhnikovTrackerComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnImplanted(Entity<SpasezhnikovImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (HasComp<SandevistanUserComponent>(args.Implanted))
        {
            _popup.PopupEntity(Loc.GetString("kereznikov-incompatible-sandevistan"), args.Implanted, args.Implanted);
            _implants.ForceRemove(args.Implanted, ent.Owner);
            return;
        }

        var tracker = EnsureComp<SpasezhnikovTrackerComponent>(args.Implanted);
        tracker.Duration = ent.Comp.Duration;
        tracker.HpThreshold = ent.Comp.HpThresholdBeforeCrit;
        tracker.MovementSpeedModifier = ent.Comp.MovementSpeedModifier;
        tracker.AttackSpeedModifier = ent.Comp.AttackSpeedModifier;
    }

    private void OnRemoved(Entity<SpasezhnikovImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<SpasezhnikovTrackerComponent>(args.Implanted);
    }

    private void OnDamageChanged(Entity<SpasezhnikovTrackerComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        foreach (var (_, amount) in args.DamageDelta.DamageDict)
            ent.Comp.CurrentDamage += (float) amount;

        if (ent.Comp.CurrentDamage < 0f)
            ent.Comp.CurrentDamage = 0f;

        if (!_mobThreshold.TryGetThresholdForState(ent.Owner, MobState.Critical, out var critThreshold))
            return;

        var threshold = (float)critThreshold!.Value - ent.Comp.HpThreshold;

        if (!ent.Comp.WasTriggered && ent.Comp.CurrentDamage >= threshold)
        {
            if (HasComp<ActiveSandevistanUserComponent>(ent.Owner) || HasComp<KereznikovActiveComponent>(ent.Owner))
                return;

            ent.Comp.WasTriggered = true;

            var active = EnsureComp<KereznikovActiveComponent>(ent.Owner);
            active.EndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
            active.MovementSpeedModifier = ent.Comp.MovementSpeedModifier;
            active.AttackSpeedModifier = ent.Comp.AttackSpeedModifier;

            _speed.RefreshMovementSpeedModifiers(ent.Owner);
        }
        else if (ent.Comp.WasTriggered && ent.Comp.CurrentDamage < threshold)
        {
            ent.Comp.WasTriggered = false;
        }
    }
}
