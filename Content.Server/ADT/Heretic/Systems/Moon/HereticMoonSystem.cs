using Content.Server.Actions;
using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Station;
using Content.Shared.Chat;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using OldStatusEffectsComponent = Content.Shared.StatusEffect.StatusEffectsComponent;
using OldStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;
using Content.Shared.ADT.Heretic.Systems.Moon;
using Content.Shared.Alert;
using Content.Server.Projectiles;
using Content.Shared.Wall;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Systems.Moon;

public sealed class HereticMoonSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly OldStatusEffectsSystem _statusOld = default!;
    [Dependency] private readonly StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PullingSystem _pull = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMoonHallucinationSystem _hallucinations = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private static readonly ProtoId<AlertPrototype> MoonBrain = "MoonBrain";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoonConvertedComponent, ComponentStartup>(OnConvertedStartup);
        SubscribeLocalEvent<MoonConvertedComponent, ComponentRemove>(OnConvertedRemove);
        SubscribeLocalEvent<MoonConvertedComponent, DamageChangedEvent>(OnConvertedDamage);
        SubscribeLocalEvent<MoonMarkStatusComponent, DamageChangedEvent>(OnMarkDamage);
        SubscribeLocalEvent<MoonMarkStatusComponent, ComponentStartup>(OnMarkStartup);
        SubscribeLocalEvent<MoonMarkStatusComponent, ComponentRemove>(OnMarkRemove);
        SubscribeLocalEvent<MoonParadeStatusComponent, DamageChangedEvent>(OnParadeDamage);
        SubscribeLocalEvent<MoonParadeStatusComponent, UpdateCanMoveEvent>(OnParadeCanMove);
        SubscribeLocalEvent<MoonIllusionComponent, DamageChangedEvent>(OnIllusionDamage);
        SubscribeLocalEvent<LunarParadeProjectileComponent, ProjectileHitEvent>(OnParadeHit);
        SubscribeLocalEvent<LunarParadeProjectileComponent, EntityTerminatingEvent>(OnParadeGone);
        SubscribeLocalEvent<LunarParadeProjectileComponent, StartCollideEvent>(OnParadeCollide, after: [typeof(ProjectileSystem)]);
        SubscribeLocalEvent<LunarParadeProjectileComponent, PreventCollideEvent>(OnParadePreventCollide);
        SubscribeLocalEvent<MoonParadeStatusComponent, ComponentRemove>(OnParadeRemove);
        SubscribeLocalEvent<HereticSanityComponent, ExaminedEvent>(OnSanityExamined);
        SubscribeLocalEvent<HereticSanityComponent, ComponentRemove>(OnSanityRemove);
        SubscribeLocalEvent<DamageableComponent, BeforeStatusEffectAddedEvent>(OnBeforeMoonDebuff);
    }

    private static readonly HashSet<string> MoonDebuffs = new()
    {
        "MoonConfusedStatusEffect",
        "MoonMarkStatusEffect",
        "MoonParadeStatusEffect",
        "MoonConvertedStatusEffect",
        "MoonSleptStatusEffect",
    };

    private void OnBeforeMoonDebuff(Entity<DamageableComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Cancelled)
            return;
        if (!MoonDebuffs.Contains(args.Effect.Id))
            return;
        if (_heretic.IsHereticOrGhoul(ent) || HasComp<MoonLunaticComponent>(ent))
            args.Cancelled = true;
    }

    private void OnSanityExamined(Entity<HereticSanityComponent> ent, ref ExaminedEvent args)
    {
        var frac = ent.Comp.Max > 0f ? ent.Comp.Current / ent.Comp.Max : 1f;
        var key = frac > 0.66f
            ? "heretic-moon-sanity-calm"
            : frac > 0.33f
                ? "heretic-moon-sanity-uneasy"
                : frac > 0.16f
                    ? "heretic-moon-sanity-fraying"
                    : "heretic-moon-sanity-shattered";
        args.PushMarkup(Loc.GetString(key));
    }

    private void OnConvertedStartup(Entity<MoonConvertedComponent> ent, ref ComponentStartup args)
    {
        _statusNew.TrySetStatusEffectDuration(ent, "StatusEffectMuted", TimeSpan.FromHours(1));
        _popup.PopupEntity(Loc.GetString("heretic-moon-converted-popup"), ent, ent);
    }

    private void OnConvertedRemove(Entity<MoonConvertedComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;
        _statusNew.TryRemoveStatusEffect(ent, "StatusEffectMuted");
    }

    private void OnConvertedDamage(Entity<MoonConvertedComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;
        ent.Comp.DamageSustained += args.DamageDelta.GetTotal().Float();
        if (ent.Comp.DamageSustained < ent.Comp.ReleaseThreshold)
            return;
        RemCompDeferred(ent, ent.Comp);
        _statusNew.TryRemoveStatusEffect(ent, "MoonConvertedStatusEffect");
        RemCompDeferred<MoonLunaticComponent>(ent);
    }

    private void OnMarkStartup(Entity<MoonMarkStatusComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out OldStatusEffectsComponent? status))
            return;
        _statusOld.TryAddStatusEffect<Content.Shared.CombatMode.Pacification.PacifiedComponent>(ent, "Pacified", TimeSpan.FromSeconds(30), true, status);
        _chat.TryEmoteWithoutChat(ent, "Laugh");
        _popup.PopupEntity(Loc.GetString("heretic-moon-mark-popup"), ent, ent);
    }

    private void OnMarkRemove(Entity<MoonMarkStatusComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (TryComp(ent, out OldStatusEffectsComponent? status))
            _statusOld.TryRemoveStatusEffect(ent, "Pacified", status);
    }

    private void OnMarkDamage(Entity<MoonMarkStatusComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;
        ent.Comp.DamageSustained += args.DamageDelta.GetTotal().Float();
        if (ent.Comp.DamageSustained < ent.Comp.ReleaseThreshold)
            return;
        RemCompDeferred(ent, ent.Comp);
        _statusNew.TryRemoveStatusEffect(ent, "MoonMarkStatusEffect");
        _popup.PopupEntity(Loc.GetString("heretic-moon-mark-release"), ent, ent);
    }

    private void OnParadeDamage(Entity<MoonParadeStatusComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;
        ent.Comp.DamageSustained += args.DamageDelta.GetTotal().Float();
        if (ent.Comp.DamageSustained < 50f)
            return;
        RemCompDeferred(ent, ent.Comp);
        _statusNew.TryRemoveStatusEffect(ent, "MoonParadeStatusEffect");
        _blocker.UpdateCanMove(ent.Owner);
    }

    private void OnParadeCanMove(Entity<MoonParadeStatusComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnParadePreventCollide(Entity<LunarParadeProjectileComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;
        if (HasComp<WallComponent>(other))
            return;
        if (TryComp<DoorComponent>(other, out var door) && door.State != DoorState.Open)
            return;
        if (HasComp<MobStateComponent>(other) && !_mobState.IsDead(other))
            return;

        args.Cancelled = true;
    }

    private void OnParadeHit(Entity<LunarParadeProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (TryComp<ProjectileComponent>(ent, out var proj))
        {
            proj.ProjectileSpent = false;
            Dirty(ent, proj);
        }
        var target = args.Target;
        var shooter = args.Shooter;
        if (target == shooter)
            return;
        if (!HasComp<MobStateComponent>(target))
            return;
        if (_mobState.IsDead(target))
            return;
        if (shooter != null && target == shooter)
            return;
        if (_heretic.IsHereticOrGhoul(target) || HasComp<MoonLunaticComponent>(target))
            return;
        if (shooter != null && _heretic.TryGetHereticComponent(shooter.Value, out _, out var shooterMind))
        {
            if (_heretic.TryGetHereticComponent(target, out _, out var targetMind) && shooterMind == targetMind)
                return;
        }
        if (!ent.Comp.Hit.Add(target))
            return;
        _statusNew.TryAddStatusEffectDuration(target, "MoonParadeStatusEffect", TimeSpan.FromSeconds(20));
        var parade = EnsureComp<MoonParadeStatusComponent>(target);
        parade.Parade = GetNetEntity(ent);
        Dirty(target, parade);
        _blocker.UpdateCanMove(target);
        _hallucinations.CauseHallucinations(target, 1, 15f, false);
        if (TryComp(target, out HereticSanityComponent? sanity))
        {
            sanity.Current = Math.Max(0f, sanity.Current - 20f);
            Dirty(target, sanity);
        }
        else
        {
            var fresh = EnsureComp<HereticSanityComponent>(target);
            fresh.Current = Math.Max(0f, fresh.Current - 20f);
            Dirty(target, fresh);
        }
        if (shooter is { } sh && _heretic.TryGetHereticComponent(sh, out var heretic, out _) && heretic.CurrentPath == "Moon" && heretic.PathStage >= 7)
        {
            var amulet = EnsureComp<MoonAmuletComponent>(sh);
            _ = amulet;
        }
    }

    private void OnParadeGone(Entity<LunarParadeProjectileComponent> ent, ref EntityTerminatingEvent args)
    {
        var netEnt = GetNetEntity(ent);
        var query = EntityQueryEnumerator<MoonParadeStatusComponent>();
        while (query.MoveNext(out var victim, out var comp))
        {
            if (comp.Parade == netEnt)
            {
                RemCompDeferred<MoonParadeStatusComponent>(victim);
                _statusNew.TryRemoveStatusEffect(victim, "MoonParadeStatusEffect");
                _blocker.UpdateCanMove(victim);
            }
        }
    }

    private void OnParadeCollide(Entity<LunarParadeProjectileComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != "projectile")
            return;

        if (TryComp<ProjectileComponent>(ent, out var proj))
        {
            proj.ProjectileSpent = false;
            Dirty(ent, proj);
        }
        if (ent.Comp.Bounces >= ent.Comp.MaxBounces)
        {
            QueueDel(ent);
            return;
        }

        var other = args.OtherEntity;
        if (!HasComp<WallComponent>(other) && !(TryComp<DoorComponent>(other, out var door) && door.State != DoorState.Open))
            return;
        if (args.OtherFixture.Hard != true)
            return;

        var normal = args.WorldNormal;
        if (normal == System.Numerics.Vector2.Zero)
        {
            var projPos = _transform.GetWorldPosition(ent);
            var otherPos = _transform.GetWorldPosition(other);
            var diff = projPos - otherPos;
            if (MathF.Abs(diff.X) > MathF.Abs(diff.Y))
                normal = new System.Numerics.Vector2(MathF.Sign(diff.X), 0);
            else if (MathF.Abs(diff.Y) > 0.001f)
                normal = new System.Numerics.Vector2(0, MathF.Sign(diff.Y));
        }

        var vel = args.OurBody.LinearVelocity;
        if (vel == System.Numerics.Vector2.Zero)
            return;

        if (normal == System.Numerics.Vector2.Zero)
            normal = -System.Numerics.Vector2.Normalize(vel);

        var reflected = vel - 2f * System.Numerics.Vector2.Dot(vel, normal) * normal;
        if (reflected == System.Numerics.Vector2.Zero || reflected.LengthSquared() < 0.01f)
            reflected = -vel;

        _physics.SetLinearVelocity(ent, reflected, body: args.OurBody);
        _physics.SetAngularVelocity(ent, 0f, body: args.OurBody);
        _transform.SetWorldRotation(ent, reflected.ToWorldAngle());
        ent.Comp.Bounces++;
        Dirty(ent, ent.Comp);
    }

    private void OnSanityRemove(Entity<HereticSanityComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent.Owner, MoonBrain);
    }

    private void OnParadeRemove(Entity<MoonParadeStatusComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<PullableComponent>(ent.Owner, out var pullable))
            _pull.TryStopPull(ent.Owner, pullable);
        _blocker.UpdateCanMove(ent.Owner);
    }

    private void OnIllusionDamage(Entity<MoonIllusionComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;
        var attacker = args.Origin;
        if (attacker != null && _heretic.IsHereticOrGhoul(attacker.Value))
            return;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Heretic/party_horn.ogg"), ent);
        var caster = ent.Comp.Caster != null ? GetEntity(ent.Comp.Caster.Value) : EntityUid.Invalid;
        var channel = caster != EntityUid.Invalid
            && TryComp<MoonPassiveComponent>(caster, out var passiveComp)
            && passiveComp.AmuletEquipped;
        var coords = _transform.GetMapCoordinates(ent);
        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 3f))
        {
            if (_heretic.IsHereticOrGhoul(mob) || HasComp<MoonLunaticComponent>(mob))
                continue;
            if (channel)
                EntityManager.System<HereticMoonAbilitySystem>().ChannelAmulet(caster, mob);
            _stun.TryUpdateParalyzeDuration(mob.Owner, TimeSpan.FromSeconds(1));
            _stun.TryKnockdown((mob.Owner, null), TimeSpan.FromSeconds(1), true);
            var spec = new DamageSpecifier();
            spec.DamageDict.Add("Cellular", 50f);
            _damage.TryChangeDamage(mob.Owner, spec, true, origin: ent);
            var sanity = EnsureComp<HereticSanityComponent>(mob);
            sanity.Current = Math.Max(0f, sanity.Current - 50f);
            Dirty(mob, sanity);
        }
        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<HereticSanityComponent>();
        while (query.MoveNext(out var uid, out var sanity))
        {
            if (sanity.Current < sanity.Max)
            {
                sanity.Current = Math.Min(sanity.Max, sanity.Current + sanity.RegenPerSecond * frameTime);
                Dirty(uid, sanity);
            }

            if ((_heretic.TryGetHereticComponent(uid, out var hComp, out _) && hComp.CurrentPath == "Moon") || HasComp<MoonLunaticComponent>(uid))
            {
                short severity = (short) Math.Clamp(1 + (int)((sanity.Max - sanity.Current) / (sanity.Max / 5f)), 1, 6);
                _alerts.ShowAlert(uid, MoonBrain, severity);
            }
        }
        var paradeQuery = EntityQueryEnumerator<MoonParadeStatusComponent, TransformComponent, PhysicsComponent>();
        while (paradeQuery.MoveNext(out var uid, out var parade, out var xform, out var physics))
        {
            if (parade.Parade == null)
                continue;
            var paradeEnt = GetEntity(parade.Parade.Value);
            if (!Exists(paradeEnt) || TerminatingOrDeleted(paradeEnt))
            {
                RemCompDeferred<MoonParadeStatusComponent>(uid);
                _statusNew.TryRemoveStatusEffect(uid, "MoonParadeStatusEffect");
                continue;
            }

            if (xform.MapID != Transform(paradeEnt).MapID)
                continue;

            var mobPos = _transform.GetWorldPosition(xform);
            var targetPos = _transform.GetWorldPosition(paradeEnt);
            var diff = targetPos - mobPos;
            var dist = diff.Length();
            if (dist > 0.5f)
            {
                var dir = diff / dist;
                var speed = Math.Min(dist * 4f, 6f);
                _physics.SetLinearVelocity(uid, dir * speed, body: physics);
            }
        }
        var passiveQuery = EntityQueryEnumerator<MoonPassiveComponent, HereticSanityComponent, DamageableComponent>();
        while (passiveQuery.MoveNext(out var uid, out var passive, out var sanityComp, out var damageable))
        {
            if (!_heretic.TryGetHereticComponent(uid, out var heretic, out _))
                continue;
            var hasAmulet = false;
            if (_inventory.TryGetSlotEntity(uid, "neck", out var neck) && HasComp<MoonAmuletComponent>(neck))
                hasAmulet = true;
            passive.AmuletEquipped = hasAmulet;
            Dirty(uid, passive);
            var level = Math.Max(1, heretic.PathStage);
            passive.Accumulator += frameTime;
            if (passive.Accumulator >= 1f)
            {
                var seconds = passive.Accumulator;
                passive.Accumulator = 0f;
                var heal = Math.Max(0.5f, level * 0.5f * seconds);
                if (!passive.AmuletEquipped)
                    heal *= 0.5f;
                if (heretic.Ascended)
                    heal = 5f * seconds;
                var spec = new DamageSpecifier();
                spec.DamageDict.Add("Cellular", -FixedPoint2.New(heal));
                _damage.TryChangeDamage(uid, spec, true, false);
            }
            if (heretic.PathStage >= 4 && TryComp(uid, out OldStatusEffectsComponent? status))
            {
                _statusOld.TryRemoveStatusEffect(uid, "ForcedSleep", status);
                _statusNew.TryRemoveStatusEffect(uid, "StatusEffectNewbornEther");
            }
        }
        var auraQuery = EntityQueryEnumerator<MoonAscensionAuraComponent, TransformComponent>();
        while (auraQuery.MoveNext(out var uid, out var aura, out var xform))
        {
            aura.Accumulator += frameTime;
            if (aura.Accumulator < aura.TickInterval)
                continue;
            aura.Accumulator = 0f;
            var heretic = _transform.GetParentUid(uid);
            if (!Exists(heretic))
                heretic = uid;
            var coords = _transform.GetMapCoordinates(uid);
            foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, aura.Radius))
            {
                if (mob.Owner == heretic)
                    continue;
                if (_mobState.IsDead(mob))
                    continue;
                if (_heretic.IsHereticOrGhoul(mob) || HasComp<MoonLunaticComponent>(mob))
                    continue;
                if (!HasComp<ActorComponent>(mob))
                    continue;
                if (_station.GetOwningStation(mob) != _station.GetOwningStation(heretic))
                    continue;
                if (_random.Prob(0.25f))
                    _hallucinations.CauseHallucinations(mob, 1, 15f, false);
                var sanity = EnsureComp<HereticSanityComponent>(mob);
                sanity.Current = Math.Max(0f, sanity.Current - aura.SanityDamage);
                Dirty(mob, sanity);
                if (_statusNew.TryGetStatusEffect(mob, "MoonConfusedStatusEffect", out _))
                    _popup.PopupEntity(Loc.GetString("heretic-moon-aura-cacophony"), mob, mob);
                _statusNew.TryAddStatusEffectDuration(mob, "MoonConfusedStatusEffect", TimeSpan.FromSeconds(2));
                _statusNew.TryAddStatusEffectDuration(mob, BlindnessSystem.BlindingStatusEffect, TimeSpan.FromSeconds(5));
                if (sanity.Current > aura.ConvertThreshold)
                    continue;
                if (HasComp<MindShieldComponent>(mob))
                {
                    _popup.PopupEntity(Loc.GetString("heretic-moon-aura-mindshield"), mob, mob, PopupType.Large);
                    var blast = _transform.GetMapCoordinates(mob);
                    var boom = new DamageSpecifier();
                    boom.DamageDict.Add("Blunt", 100f);
                    boom.DamageDict.Add("Piercing", 100f);
                    foreach (var caught in _lookup.GetEntitiesInRange<MobStateComponent>(blast, 1f))
                        _damage.TryChangeDamage(caught.Owner, boom, true, false, origin: heretic);
                    continue;
                }
                EntityManager.System<HereticMoonAbilitySystem>().ConvertToLunatic(mob, heretic);
            }
        }
}

}