using System.Numerics;
using Content.Shared.ADT.Implants.Sandevistan;
using Content.Shared.ADT.Implants.ImplantVision;
using Content.Shared.ADT.Trail;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Implants.ImplantBoost;

public sealed class ImplantBoostSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly ImplantVisionSystem _vision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantBoostComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<ImplantBoostComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImplantBoostComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ImplantBoostComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<ImplantBoostComponent, ModifyDoAfterDelayEvent>(OnModifyDoAfterDelay);
        SubscribeLocalEvent<ImplantBoostComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ImplantBoostComponent, ImplantEmpAffectedEvent>(OnEmp);
    }

    public void Boost(EntityUid uid, ImplantBoostSettings settings, TimeSpan? duration = null)
    {
        if (_net.IsClient)
            return;

        RemComp<ImplantBoostComponent>(uid);

        var comp = AddComp<ImplantBoostComponent>(uid);
        comp.Settings = settings;
        comp.MovementSpeedModifier = settings.MovementSpeedModifier;
        comp.AttackSpeedModifier = settings.AttackSpeedModifier;
        comp.DoAfterSpeedModifier = settings.DoAfterSpeedModifier;
        comp.EndTime = duration == null ? null : _timing.CurTime + duration.Value;
        Dirty(uid, comp);

        if (settings.Vision is { } vision)
            _vision.Add(uid, vision);

        if (settings.Trail is { } trailSettings && !HasComp<TrailComponent>(uid))
        {
            var trail = AddComp<TrailComponent>(uid);
            trail.RenderedEntity = uid;
            trail.LerpTime = trailSettings.LerpTime;
            trail.LerpDelay = trailSettings.LerpDelay;
            trail.Lifetime = trailSettings.Lifetime;
            trail.Frequency = trailSettings.Frequency;
            trail.AlphaLerpAmount = trailSettings.AlphaLerpAmount;
            trail.MaxParticleAmount = trailSettings.MaxParticleAmount;

            if (trailSettings.Color is { } color)
                trail.Color = color;
        }

        EntityManager.AddComponents(uid, settings.Components);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    public void Stop(EntityUid uid)
    {
        RemCompDeferred<ImplantBoostComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var expired = new ValueList<EntityUid>();
        var query = EntityQueryEnumerator<ImplantBoostComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime is { } end && end <= curTime)
            {
                expired.Add(uid);
                continue;
            }

            if (comp.Settings?.Trail is not { Rainbow: true } || !TryComp<TrailComponent>(uid, out var trail))
                continue;

            trail.Color = Color.FromHsv(new Vector4(comp.ColorAccumulator % 100f / 100f, 1, 1, 1));
            comp.ColorAccumulator++;
            Dirty(uid, trail);
        }

        foreach (var uid in expired)
            RemCompDeferred<ImplantBoostComponent>(uid);
    }

    private void OnState(Entity<ImplantBoostComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnShutdown(Entity<ImplantBoostComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        if (_net.IsClient || ent.Comp.Settings is not { } settings)
            return;

        if (settings.Vision is { } vision)
            _vision.Remove(ent.Owner, vision);

        if (settings.Trail != null)
            RemComp<TrailComponent>(ent.Owner);

        EntityManager.RemoveComponents(ent.Owner, settings.Components);
    }

    private void OnRefreshSpeed(Entity<ImplantBoostComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping || ent.Comp.MovementSpeedModifier == 1f)
            return;

        args.ModifySpeed(ent.Comp.MovementSpeedModifier, ent.Comp.MovementSpeedModifier);
    }

    private void OnMeleeAttack(Entity<ImplantBoostComponent> ent, ref MeleeAttackEvent args)
    {
        if (ent.Comp.AttackSpeedModifier == 1f || !TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon))
            return;

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate - rate / ent.Comp.AttackSpeedModifier;
    }

    private void OnModifyDoAfterDelay(Entity<ImplantBoostComponent> ent, ref ModifyDoAfterDelayEvent args)
    {
        if (ent.Comp.DoAfterSpeedModifier == 1f)
            return;

        args.Delay = args.Delay / ent.Comp.DoAfterSpeedModifier;
    }

    private void OnMobStateChanged(Entity<ImplantBoostComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsClient || ent.Comp.Settings is { RemoveOnMobStateChange: false })
            return;

        Stop(ent.Owner);
    }

    private void OnEmp(Entity<ImplantBoostComponent> ent, ref ImplantEmpAffectedEvent args)
    {
        Stop(ent.Owner);
    }
}
