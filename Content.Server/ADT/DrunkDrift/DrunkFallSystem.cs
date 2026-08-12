using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.DrunkDrift;
using Content.Shared.Drunk;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.DrunkDrift;

/// <summary>
///     ADT: падения пьяных. Раз в секунду у движущегося пьяного есть шанс
///     споткнуться и упасть (нокдаун). Предметы из рук при падении выпадают сами.
/// </summary>
public sealed class DrunkFallSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<ADTDrunkDriftComponent> _drunkQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<KnockedDownComponent> _knockedQuery;

    private float _accumulator;

    public override void Initialize()
    {
        _drunkQuery = GetEntityQuery<ADTDrunkDriftComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _knockedQuery = GetEntityQuery<KnockedDownComponent>();

        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectAppliedEvent>(OnDrunkApplied);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
        SubscribeLocalEvent<ADTDrunkDriftComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDrunkApplied(Entity<DrunkStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var comp = EnsureComp<ADTDrunkDriftComponent>(args.Target);
        UpdateVisuals(args.Target, comp);
    }

    private void OnDrunkRemoved(Entity<DrunkStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        // Пока остался хоть один эффект опьянения - маркер держим и обновляем визуал.
        if (_statusEffects.HasEffectComp<DrunkStatusEffectComponent>(args.Target))
        {
            if (TryComp<ADTDrunkDriftComponent>(args.Target, out var comp))
                UpdateVisuals(args.Target, comp);

            return;
        }

        RemComp<ADTDrunkDriftComponent>(args.Target);
    }

    /// <summary>
    ///     Пересчитывает активность пьяных эффектов по остатку опьянения:
    ///     они работают только когда началось размытие экрана (порог adt.drunk_visual_threshold).
    /// </summary>
    private void UpdateVisuals(EntityUid uid, ADTDrunkDriftComponent comp)
    {
        var active = false;
        if (_statusEffects.TryGetMaxTime<DrunkStatusEffectComponent>(uid, out var time))
        {
            var remaining = time.EndEffectTime - _timing.CurTime;
            active = remaining == null
                || remaining.Value >= TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.DrunkVisualThreshold));
        }

        if (comp.VisualsActive == active)
            return;

        comp.VisualsActive = active;
        Dirty(uid, comp);
    }

    private void OnExamined(Entity<ADTDrunkDriftComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.VisualsActive)
            return;

        args.PushMarkup(Loc.GetString("adt-drunk-examine", ("ent", ent.Owner)));
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < 1f)
            return;

        _accumulator -= 1f;

        var chance = _cfg.GetCVar(ADTCCVars.DrunkFallChance);
        if (chance <= 0f)
            return;

        var cooldown = TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.DrunkFallCooldown));
        var knockdownTime = TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.DrunkKnockdownTime));
        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<ADTDrunkDriftComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Остаток опьянения тает: пересчитываем, идёт ли размытие экрана.
            UpdateVisuals(uid, comp);
            if (!comp.VisualsActive)
                continue;

            // Кулдаун после падения.
            if (comp.NextFall > now)
                continue;

            // Только живые на ногах.
            if (!_mobQuery.TryComp(uid, out var mob) || mob.CurrentState != MobState.Alive)
                continue;

            if (_knockedQuery.HasComp(uid))
                continue;

            if (!_physicsQuery.TryComp(uid, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                continue;

            // Падать можно только в движении.
            if (physics.LinearVelocity.Length() < 0.5f)
                continue;

            if (!_random.Prob(chance))
                continue;

            comp.NextFall = now + cooldown;
            Dirty(uid, comp);
            _stun.TryKnockdown(uid, knockdownTime, force: true);
            _popup.PopupEntity(Loc.GetString($"adt-drunk-fall-{_random.Next(1, 11)}", ("ent", uid)), uid);
        }
    }
}
