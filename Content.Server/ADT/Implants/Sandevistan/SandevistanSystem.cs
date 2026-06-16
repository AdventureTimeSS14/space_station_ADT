using Content.Shared.ADT.Implants.Sandevistan;
using Content.Shared.ADT.Trail;
using Content.Shared.ADT.Glitch;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.ADT.Implants;
using Content.Shared.DoAfter;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.ADT.Implants.Sandevistan;

public sealed class SandevistanSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string SlowfieldFixtureId = "sandevistan-slowfield";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanUserComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SandevistanUserComponent, ToggleSandevistanEvent>(OnToggle);
        SubscribeLocalEvent<SandevistanUserComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SandevistanUserComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<SandevistanUserComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SandevistanUserComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SandevistanUserComponent, ImplantEmpAffectedEvent>(OnEmpPulse);
        SubscribeLocalEvent<SandevistanUserComponent, ModifyDoAfterDelayEvent>(OnModifyDoAfterDelay);

        SubscribeLocalEvent<ActiveSandevistanUserComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ActiveSandevistanUserComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ActiveSandevistanUserComponent, PreventCollideEvent>(OnPreventCollide);

        SubscribeLocalEvent<SandevistanSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedRefreshSpeed);
        SubscribeLocalEvent<SandevistanSlowedComponent, RemoveSandevistanSlowdownEvent>(OnRemoveSlowdown);

        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnPhysicsUpdateAfterSolve);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var glitchQuery = EntityQueryEnumerator<GlitchComponent>();
        while (glitchQuery.MoveNext(out var glitchUid, out var glitch))
        {
            if (_timing.CurTime >= glitch.ExpiresAt)
                RemCompDeferred<GlitchComponent>(glitchUid);
        }

        var query = EntityQueryEnumerator<ActiveSandevistanUserComponent, SandevistanUserComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (comp.DisableAt != null && _timing.CurTime > comp.DisableAt)
                Disable(uid, comp);

            if (comp.Trail != null)
            {
                comp.Trail.Color = Color.FromHsv(new Vector4(comp.ColorAccumulator % 100f / 100f, 1, 1, 1));
                comp.ColorAccumulator++;
                Dirty(uid, comp.Trail);
            }

            comp.CurrentLoad += comp.LoadPerActiveSecond * frameTime;

            if (TryComp<SandevistanLoadComponent>(uid, out var loadComp))
            {
                loadComp.CurrentLoad = comp.CurrentLoad;
                Dirty(uid, loadComp);
            }

            var stateActions = new Dictionary<int, Action>
            {
                { 1, () => _jittering.DoJitter(uid, comp.StatusEffectTime, true)},
                { 2, () => _stamina.TakeStaminaDamage(uid, comp.StaminaDamage * frameTime)},
                { 3, () => {
                    _damageable.TryChangeDamage(uid, comp.Damage * frameTime, ignoreResistances: true);
                    var glitch = EnsureComp<GlitchComponent>(uid);
                    glitch.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(2);
                    Dirty(uid, glitch);
                }},
                { 4, () => _stun.TryKnockdown(uid, comp.StatusEffectTime, true)},
                { 5, () => TriggerOverloadGlitch(uid, comp)},
                { 6, () => _damageable.TryChangeDamage(uid, comp.Damage * frameTime, ignoreResistances: true)},
            };

            var filteredStates = new List<int>();
            foreach (var stateThreshold in comp.Thresholds)
                if (comp.CurrentLoad >= stateThreshold.Value)
                    filteredStates.Add((int)stateThreshold.Key);

            filteredStates.Sort((a, b) => b.CompareTo(a));
            foreach (var state in filteredStates)
                if (stateActions.TryGetValue(state, out var action))
                    action();

            if (comp.NextPopupTime > _timing.CurTime)
                continue;

            var popup = -1;
            foreach (var state in filteredStates)
                if (state > popup && state < 4)
                    popup = state;

            if (popup == -1)
                continue;

            _popup.PopupCursor(Loc.GetString("sandevistan-overload-" + popup), uid, PopupType.LargeCaution);
            comp.NextPopupTime = _timing.CurTime + comp.PopupDelay;
        }
    }

    private void OnStartup(Entity<SandevistanUserComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ActionUid = _actions.AddAction(ent, ent.Comp.ActionProto);
        ent.Comp.EmpLastPulse = _timing.CurTime - ent.Comp.EmpCooldown;
    }

    private void OnToggle(Entity<SandevistanUserComponent> ent, ref ToggleSandevistanEvent args)
    {
        if (_timing.CurTime < ent.Comp.EmpLastPulse + ent.Comp.EmpCooldown)
        {
            args.Handled = true;
            return;
        }

        args.Handled = true;

        if (ent.Comp.Active != null)
        {
            _audio.Stop(ent.Comp.RunningSound);
            _audio.PlayEntity(ent.Comp.EndSound, ent, ent);
            ent.Comp.DisableAt = _timing.CurTime + ent.Comp.ShiftDelay;
            return;
        }

        ent.Comp.Active = EnsureComp<ActiveSandevistanUserComponent>(ent);
        ent.Comp.CurrentLoad = MathF.Max(0, ent.Comp.CurrentLoad + ent.Comp.LoadPerInactiveSecond * (float)(_timing.CurTime - ent.Comp.LastEnabled).TotalSeconds);
        var loadComp = EnsureComp<SandevistanLoadComponent>(ent);
        loadComp.LoadAlert = ent.Comp.LoadAlert;
        _alerts.ShowAlert(ent.Owner, ent.Comp.LoadAlert);
        _speed.RefreshMovementSpeedModifiers(ent);

        if (ent.Comp.SlowfieldEnabled)
            CreateSlowfieldFixture(ent, ent.Comp);

        if (!HasComp<TrailComponent>(ent))
        {
            var trail = AddComp<TrailComponent>(ent);
            trail.RenderedEntity = ent;
            trail.LerpTime = 0.05f;
            trail.LerpDelay = TimeSpan.FromSeconds(1);
            trail.Lifetime = 0.5f;
            trail.Frequency = 0.06f;
            trail.AlphaLerpAmount = 0.3f;
            trail.MaxParticleAmount = 15;
            ent.Comp.Trail = trail;
        }

        if (!HasComp<SandevistanVisionComponent>(ent))
            ent.Comp.Overlay = AddComp<SandevistanVisionComponent>(ent);

        var audio = _audio.PlayEntity(ent.Comp.StartSound, ent, ent);
        if (!audio.HasValue)
            return;

        ent.Comp.RunningSound = audio.Value.Entity;
    }

    private void OnRefreshSpeed(Entity<SandevistanUserComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Active != null)
            args.ModifySpeed(ent.Comp.MovementSpeedModifier, ent.Comp.MovementSpeedModifier);
    }

    private void OnMeleeAttack(Entity<SandevistanUserComponent> ent, ref MeleeAttackEvent args)
    {
        if (ent.Comp.Active == null
            || !TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon))
            return;

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate - rate / ent.Comp.AttackSpeedModifier;
    }

    private void OnMobStateChanged(Entity<SandevistanUserComponent> ent, ref MobStateChangedEvent args) =>
        Disable(ent, ent.Comp);

    private void OnShutdown(Entity<SandevistanUserComponent> ent, ref ComponentShutdown args)
    {
        Disable(ent, ent.Comp);
        Del(ent.Comp.ActionUid);
    }

    // Slowfield — fixture-based

    private void CreateSlowfieldFixture(EntityUid uid, SandevistanUserComponent comp)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var shape = new PhysShapeCircle(comp.SlowfieldRadius);
        _fixtures.TryCreateFixture(uid, shape, SlowfieldFixtureId,
            collisionLayer: (int)CollisionGroup.ThrownItem,
            collisionMask: (int)(CollisionGroup.MobMask | CollisionGroup.BulletImpassable | CollisionGroup.ThrownItem),
            hard: false, body: physics);
    }

    private void DestroySlowfieldFixture(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        _fixtures.DestroyFixture(uid, SlowfieldFixtureId, body: physics);
    }

    private void OnStartCollide(Entity<ActiveSandevistanUserComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<SandevistanUserComponent>(ent, out var comp) || !comp.SlowfieldEnabled)
            return;

        if (args.OurFixtureId != SlowfieldFixtureId || args.OtherEntity == ent.Owner)
            return;

        ApplySlowdown(ent.Owner, args.OtherEntity, comp);
    }

    private void OnEndCollide(Entity<ActiveSandevistanUserComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != SlowfieldFixtureId)
            return;

        if (!TryComp<SandevistanSlowedComponent>(args.OtherEntity, out var slowed) || slowed.Source != ent.Owner)
            return;

        var ev = new RemoveSandevistanSlowdownEvent(ent.Owner);
        RaiseLocalEvent(args.OtherEntity, ref ev);
    }

    private void OnPreventCollide(Entity<ActiveSandevistanUserComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(SlowfieldFixtureId, out var slowfieldFixture)
            || args.OurFixture != slowfieldFixture)
            return;

        if (HasComp<DoorComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void ApplySlowdown(EntityUid source, EntityUid target, SandevistanUserComponent comp)
    {
        if (TryComp<SandevistanSlowedComponent>(target, out var existing) && existing.IsSlowed)
            return;

        if (HasComp<ActiveSandevistanUserComponent>(target))
            return;

        var slowed = EnsureComp<SandevistanSlowedComponent>(target);
        slowed.IsSlowed = true;
        slowed.Source = source;

        if (HasComp<MobStateComponent>(target))
        {
            slowed.SpeedMultiplier = comp.SlowfieldMobMultiplier;
            _speed.RefreshMovementSpeedModifiers(target);

            var glitch = EnsureComp<GlitchComponent>(target);
            glitch.ExpiresAt = _timing.CurTime + TimeSpan.FromHours(1);
            slowed.HadGlitch = true;
            Dirty(target, glitch);
        }
        else if (HasComp<ProjectileComponent>(target))
        {
            slowed.SpeedMultiplier = comp.SlowfieldMobMultiplier * 0.2f;
            ApplyPhysicsSlowdown(target, slowed);
        }
        else if (TryComp<ThrownItemComponent>(target, out var thrown))
        {
            slowed.SpeedMultiplier = comp.SlowfieldMobMultiplier;
            ApplyThrownItemSlowdown(target, slowed, thrown);
        }

        Dirty(target, slowed);
    }

    private void ApplyPhysicsSlowdown(EntityUid target, SandevistanSlowedComponent slowed)
    {
        if (!TryComp<PhysicsComponent>(target, out var physics))
            return;

        slowed.OriginalLinearVelocity = physics.LinearVelocity;
        _physics.SetLinearVelocity(target, physics.LinearVelocity * slowed.SpeedMultiplier, body: physics);
    }

    private void ApplyThrownItemSlowdown(EntityUid target, SandevistanSlowedComponent slowed, ThrownItemComponent thrown)
    {
        if (!TryComp<PhysicsComponent>(target, out var physics))
            return;

        slowed.OriginalLinearVelocity = physics.LinearVelocity;
        _physics.SetLinearVelocity(target, slowed.OriginalLinearVelocity * slowed.SpeedMultiplier, body: physics);

        if (thrown.LandTime != null && slowed.SpeedMultiplier > 0)
        {
            var remaining = thrown.LandTime.Value - _timing.CurTime;
            thrown.LandTime = _timing.CurTime + remaining / slowed.SpeedMultiplier;
        }
    }

    private void OnSlowedRefreshSpeed(EntityUid uid, SandevistanSlowedComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (comp.IsSlowed)
            args.ModifySpeed(comp.SpeedMultiplier, comp.SpeedMultiplier);
    }

    private void OnRemoveSlowdown(Entity<SandevistanSlowedComponent> ent, ref RemoveSandevistanSlowdownEvent args)
    {
        if (ent.Comp.Source != args.Source || !ent.Comp.IsSlowed)
            return;

        ent.Comp.IsSlowed = false;

        if (HasComp<MobStateComponent>(ent))
        {
            _speed.RefreshMovementSpeedModifiers(ent);

            if (ent.Comp.HadGlitch)
                RemCompDeferred<GlitchComponent>(ent);
        }
        else if (TryComp<PhysicsComponent>(ent, out var physics)
                 && ent.Comp.OriginalLinearVelocity.LengthSquared() > 0.01f)
        {
            _physics.SetLinearVelocity(ent, ent.Comp.OriginalLinearVelocity, body: physics);
        }

        if (TryComp<ThrownItemComponent>(ent, out var thrown)
            && thrown.LandTime != null
            && ent.Comp.SpeedMultiplier > 0)
        {
            var remaining = thrown.LandTime.Value - _timing.CurTime;
            thrown.LandTime = _timing.CurTime + remaining * ent.Comp.SpeedMultiplier;
        }

        RemCompDeferred<SandevistanSlowedComponent>(ent);
    }

    private void OnPhysicsUpdateAfterSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        var query = EntityQueryEnumerator<SandevistanSlowedComponent>();
        while (query.MoveNext(out var uid, out var slowed))
        {
            if (!slowed.IsSlowed
                || !HasComp<ThrownItemComponent>(uid)
                || slowed.OriginalLinearVelocity.LengthSquared() <= 0.01f)
                continue;

            var target = slowed.OriginalLinearVelocity * slowed.SpeedMultiplier;
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                continue;

            if ((physics.LinearVelocity - target).LengthSquared() > 0.01f)
                _physics.SetLinearVelocity(uid, target, body: physics);
        }
    }

    private void ClearSlowfield(EntityUid source)
    {
        var query = EntityQueryEnumerator<SandevistanSlowedComponent>();
        while (query.MoveNext(out var uid, out var slowed))
        {
            if (slowed.Source != source)
                continue;

            var ev = new RemoveSandevistanSlowdownEvent(source);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void Disable(EntityUid uid, SandevistanUserComponent comp)
    {
        RemComp<ActiveSandevistanUserComponent>(uid);
        comp.Active = null;
        comp.DisableAt = null;
        comp.LastEnabled = _timing.CurTime;
        comp.ColorAccumulator = 0;
        _audio.Stop(comp.RunningSound);
        _alerts.ClearAlert(uid, comp.LoadAlert);
        RemCompDeferred<SandevistanLoadComponent>(uid);

        if (comp.SlowfieldEnabled)
        {
            ClearSlowfield(uid);
            DestroySlowfieldFixture(uid);
        }

        _speed.RefreshMovementSpeedModifiers(uid);

        if (comp.Overlay != null)
        {
            RemComp(uid, comp.Overlay);
            comp.Overlay = null;
        }

        if (comp.Trail != null)
        {
            RemComp(uid, comp.Trail);
            comp.Trail = null;
        }
    }

    private void TriggerOverloadGlitch(EntityUid uid, SandevistanUserComponent comp)
    {
        var glitch = EnsureComp<GlitchComponent>(uid);
        glitch.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(3);
        Dirty(uid, glitch);
        Disable(uid, comp);
    }

    private void OnModifyDoAfterDelay(Entity<SandevistanUserComponent> ent, ref ModifyDoAfterDelayEvent args)
    {
        if (ent.Comp.Active == null)
            return;

        args.Delay = args.Delay / ent.Comp.DoAfterSpeedModifier;
    }

    private void OnEmpPulse(Entity<SandevistanUserComponent> ent, ref ImplantEmpAffectedEvent args)
    {
        ent.Comp.EmpLastPulse = _timing.CurTime;
        var uid = ent.Owner;

        ent.Comp.CurrentLoad += ent.Comp.EmpOverload;
        _damageable.TryChangeDamage(uid, ent.Comp.EmpDamage, ignoreResistances: true);
        _jittering.DoJitter(uid, TimeSpan.FromSeconds(30f), true);
        _stun.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(5f));
        Spawn("EffectSparks", Transform(uid).Coordinates);
        Disable(ent, ent.Comp);
    }
}
