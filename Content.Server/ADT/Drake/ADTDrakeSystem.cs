using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Drake;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;

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

        _megafauna.ScheduleAt(ent, _timing.CurTime, () => MeleeFollowUp(ent, living));
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
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
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
        return _megafauna.TryGetTarget(uid, out target);
    }

    public void Aggro(EntityUid uid, EntityUid target)
    {
        if (TryComp<MegafaunaComponent>(uid, out var megafauna))
            _megafauna.TryAggro((uid, megafauna), target);
    }

    public bool IsRecovering(Entity<ADTDrakeComponent> ent)
    {
        return TryComp<ADTMegafaunaAIComponent>(ent, out var ai) && _megafauna.IsRecovering((ent.Owner, ai));
    }

    public void SetRecoveryTime(Entity<ADTDrakeComponent> ent, TimeSpan buffer)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRecoveryTime((ent.Owner, ai), buffer);
    }

    public float CalculateAnger(Entity<ADTDrakeComponent> ent)
    {
        return TryComp<ADTMegafaunaAIComponent>(ent, out var ai) ? _megafauna.CalculateAnger((ent.Owner, ai)) : 0f;
    }

    public bool IsBelowHalfHealth(EntityUid uid)
    {
        if (!_thresholds.TryGetDeadThreshold(uid, out var maxHealth))
            return false;

        return _damageable.GetTotalDamage(uid) > maxHealth.Value * 0.5f;
    }

    public void Devour(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.Devour((ent.Owner, ai), target);
    }
}
