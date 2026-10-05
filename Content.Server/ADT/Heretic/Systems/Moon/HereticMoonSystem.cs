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
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using OldStatusEffectsComponent = Content.Shared.StatusEffect.StatusEffectsComponent;
using OldStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;

namespace Content.Server.ADT.Heretic.Systems.Moon;

public sealed class HereticMoonSystem : EntitySystem
{
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
        SubscribeLocalEvent<MoonIllusionComponent, DamageChangedEvent>(OnIllusionDamage);
        SubscribeLocalEvent<LunarParadeProjectileComponent, ProjectileHitEvent>(OnParadeHit);
        SubscribeLocalEvent<LunarParadeProjectileComponent, EntityTerminatingEvent>(OnParadeGone);
        SubscribeLocalEvent<LunarParadeProjectileComponent, StartCollideEvent>(OnParadeCollide);
        SubscribeLocalEvent<MoonParadeStatusComponent, ComponentRemove>(OnParadeRemove);
        SubscribeLocalEvent<HereticSanityComponent, ExaminedEvent>(OnSanityExamined);
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
        if (!TryComp(ent, out OldStatusEffectsComponent? status))
            return;
        _statusOld.TryAddStatusEffect(ent, "Muted", TimeSpan.FromHours(1), true, status);
        _popup.PopupEntity(Loc.GetString("heretic-moon-converted-popup"), ent, ent);
    }

    private void OnConvertedRemove(Entity<MoonConvertedComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (TryComp(ent, out OldStatusEffectsComponent? status))
            _statusOld.TryRemoveStatusEffect(ent, "Muted", status);
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
        _statusOld.TryAddStatusEffect(ent, "Pacified", TimeSpan.FromSeconds(30), true, status);
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
    }

    private void OnParadeHit(Entity<LunarParadeProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var target = args.Target;
        var shooter = args.Shooter;
        _statusNew.TryAddStatusEffectDuration(target, "MoonParadeStatusEffect", TimeSpan.FromSeconds(20));
        var parade = EnsureComp<MoonParadeStatusComponent>(target);
        parade.Parade = GetNetEntity(ent);
        Dirty(target, parade);
        var puller = EnsureComp<PullerComponent>(ent);
        puller.NeedsHands = false;
        Dirty(ent, puller);
        _pull.TryStartPull(ent, target, force: true);
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
        _pull.StopAllPulls(ent, stopPullable: false, stopPuller: true);
    }

    private void OnParadeCollide(Entity<LunarParadeProjectileComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Bounces >= ent.Comp.MaxBounces)
        {
            QueueDel(ent);
            return;
        }

        var other = args.OtherEntity;
        if (HasComp<MobStateComponent>(other) || HasComp<ProjectileComponent>(other))
            return;
        if (!TryComp(other, out TransformComponent? otherXform) || otherXform.Anchored != true)
            return;

        var normal = args.WorldNormal;
        if (normal == System.Numerics.Vector2.Zero)
            return;

        var vel = args.OurBody.LinearVelocity;
        var reflected = vel - 2f * System.Numerics.Vector2.Dot(vel, normal) * normal;
        if (reflected == System.Numerics.Vector2.Zero)
            return;

        _physics.SetLinearVelocity(ent, reflected, body: args.OurBody);
        ent.Comp.Bounces++;
        Dirty(ent, ent.Comp);
    }

    private void OnParadeRemove(Entity<MoonParadeStatusComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<PullableComponent>(ent.Owner, out var pullable))
            _pull.TryStopPull(ent.Owner, pullable);
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
            if (HasComp<Content.Shared.Heretic.HereticComponent>(mob) || HasComp<MoonLunaticComponent>(mob) || HasComp<Content.Shared.Heretic.GhoulComponent>(mob))
                continue;
            if (channel)
                EntityManager.System<HereticMoonAbilitySystem>().ChannelAmulet(caster, mob);
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
            var heal = level * 0.3f * frameTime;
            if (!passive.AmuletEquipped)
                heal *= 0.5f;
            if (heretic.Ascended)
                heal = 5f * frameTime;
            var spec = new DamageSpecifier();
            spec.DamageDict.Add("Cellular", -heal);
            _damage.TryChangeDamage(uid, spec, true, false);
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
            var coords = _transform.GetMapCoordinates(uid);
            foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, aura.Radius))
            {
                if (mob.Owner == uid)
                    continue;
                if (_mobState.IsDead(mob))
                    continue;
                if (HasComp<Content.Shared.Heretic.HereticComponent>(mob) || HasComp<MoonLunaticComponent>(mob) || HasComp<Content.Shared.Heretic.GhoulComponent>(mob))
                    continue;
                var sanity = EnsureComp<HereticSanityComponent>(mob);
                sanity.Current = Math.Max(0f, sanity.Current - aura.SanityDamage);
                Dirty(mob, sanity);
                _stun.TryKnockdown((mob.Owner, null), TimeSpan.FromSeconds(1), true);
                if (sanity.Current > aura.ConvertThreshold)
                    continue;
                if (HasComp<MindShieldComponent>(mob))
                {
                    var lethal = new DamageSpecifier();
                    lethal.DamageDict.Add("Cellular", 100f);
                    _damage.TryChangeDamage(mob.Owner, lethal, true);
                    continue;
                }
                _statusNew.TryAddStatusEffectDuration(mob.Owner, "MoonConvertedStatusEffect", TimeSpan.FromHours(1));
                var lunatic = EnsureComp<MoonLunaticComponent>(mob);
                lunatic.Master = GetNetEntity(uid);
                Dirty(mob, lunatic);
                EntityUid? echo = null;
                _actions.AddAction(mob.Owner, ref echo, "ActionHereticMoonlightEcho");
                EntityUid? touch = null;
                _actions.AddAction(mob.Owner, ref touch, "ActionHereticTouchOfMadness");
                Spawn("HereticMoonAmulet", Transform(mob).Coordinates);
            }
        }
    }
}
