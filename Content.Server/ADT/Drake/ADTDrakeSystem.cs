using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.ADT.Drake;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;

    private const string TargetKey = "Target";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<ADTDrakeComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTDrakeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ADTDrakeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnAttemptMelee(Entity<ADTDrakeComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!IsRecovering(ent) && !HasComp<ADTDrakeSwoopComponent>(ent))
            return;

        args.Cancelled = true;
    }

    private void OnMeleeHit(Entity<ADTDrakeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        EntityUid? living = null;

        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner)
                continue;

            if (!HasComp<MobStateComponent>(target))
                continue;

            if (!_mobState.IsDead(target))
            {
                living ??= target;
                continue;
            }

            if (!ent.Comp.CanDevour)
                continue;

            Devour(ent, target);
            return;
        }

        if (living == null)
            return;

        var sequence = EnsureComp<ADTDrakeSequenceComponent>(ent);
        sequence.Queue.Add(new ADTDrakeStep
        {
            ExecuteAt = _timing.CurTime,
            Type = ADTDrakeStepType.MeleeFollowUp,
            Target = living,
        });
    }

    public void MeleeFollowUp(Entity<ADTDrakeComponent> ent, EntityUid? target)
    {
        if (target is not { } victim || TerminatingOrDeleted(victim))
            return;

        if (_mobState.IsDead(victim))
        {
            if (ent.Comp.CanDevour)
                Devour(ent, victim);

            return;
        }

        var ev = new ADTDrakeMeleeHitLivingEvent(victim);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnMobStateChanged(Entity<ADTDrakeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _popup.PopupEntity(Loc.GetString("adt-drake-death", ("drake", ent.Owner)), ent, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.DeathSound, ent);
    }

    private void OnRefreshSpeed(Entity<ADTDrakeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.EscapeEnraged)
            args.ModifySpeed(ent.Comp.EscapeSpeedMultiplier, ent.Comp.EscapeSpeedMultiplier);
    }

    public void SetEscapeEnraged(Entity<ADTDrakeComponent> ent, bool value)
    {
        if (ent.Comp.EscapeEnraged == value)
            return;

        ent.Comp.EscapeEnraged = value;
        _speed.RefreshMovementSpeedModifiers(ent);
        _appearance.SetData(ent, ADTDrakeVisuals.EscapeEnraged, value);

        if (!_light.TryGetLight(ent, out var light))
            return;

        if (value)
        {
            ent.Comp.BaseLightRadius ??= light.Radius;
            _light.SetRadius(ent, ent.Comp.EscapeLightRadius, light);
            return;
        }

        if (ent.Comp.BaseLightRadius is { } radius)
            _light.SetRadius(ent, radius, light);
    }

    public void Heal(Entity<ADTDrakeComponent> ent, float amount)
    {
        var group = _proto.Index(ent.Comp.HealGroup);
        _damageable.TryChangeDamage(ent.Owner, new DamageSpecifier(group, -amount), true, origin: ent.Owner);
    }

    public bool TryGetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (TryComp<ADTDrakeComponent>(uid, out var drake)
            && drake.ManualTarget is { } manual
            && !TerminatingOrDeleted(manual))
        {
            target = manual;
            return true;
        }

        if (TryComp<HTNComponent>(uid, out var htn)
            && htn.Blackboard.TryGetValue<EntityUid>(TargetKey, out var found, EntityManager)
            && !TerminatingOrDeleted(found))
        {
            target = found;
            return true;
        }

        if (TryComp<MegafaunaComponent>(uid, out var megafauna)
            && megafauna.Aggressor is { } aggressor
            && !TerminatingOrDeleted(aggressor))
        {
            target = aggressor;
            return true;
        }

        return false;
    }

    public void Aggro(EntityUid uid, EntityUid target)
    {
        if (TryComp<MegafaunaComponent>(uid, out var megafauna))
            _megafauna.TryAggro((uid, megafauna), target);
    }

    public bool IsRecovering(Entity<ADTDrakeComponent> ent)
    {
        return _timing.CurTime < ent.Comp.RecoveryUntil;
    }

    public void SetRecoveryTime(Entity<ADTDrakeComponent> ent, TimeSpan buffer)
    {
        var until = _timing.CurTime + buffer;
        ent.Comp.RecoveryUntil = until;
        ent.Comp.NextRangedAt = until;
    }

    public float CalculateAnger(Entity<ADTDrakeComponent> ent)
    {
        var damageTaken = (float) _damageable.GetTotalDamage(ent.Owner);

        ent.Comp.AngerModifier = Math.Clamp(damageTaken / ent.Comp.AngerDamageDivisor, 0f, ent.Comp.MaxAnger);
        return ent.Comp.AngerModifier;
    }

    public bool IsBelowHalfHealth(EntityUid uid)
    {
        if (!_thresholds.TryGetDeadThreshold(uid, out var maxHealth))
            return false;

        return _damageable.GetTotalDamage(uid) > maxHealth.Value * 0.5f;
    }

    public void Devour(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("adt-drake-devour", ("drake", ent.Owner), ("target", target)), ent, PopupType.LargeCaution);

        var onStation = _station.GetOwningStation(ent.Owner) != null;
        if ((!onStation || HasComp<ActorComponent>(ent)) && _thresholds.TryGetDeadThreshold(target, out var maxHealth))
        {
            var group = _proto.Index(ent.Comp.HealGroup);
            var heal = new DamageSpecifier(group, -maxHealth.Value * ent.Comp.DevourHealFraction);
            _damageable.TryChangeDamage(ent.Owner, heal, true, origin: ent.Owner);
        }

        if (_gibbing.Gib(target, true, ent.Owner).Count == 0)
            QueueDel(target);
    }
}
