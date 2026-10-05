using Content.Server.Actions;
using Content.Shared.ADT.Deafness;
using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic.Moon;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Heretic.Systems.Moon;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using OldStatusEffectsComponent = Content.Shared.StatusEffect.StatusEffectsComponent;
using OldStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;

namespace Content.Server.ADT.Heretic.Systems.Moon;

public sealed class HereticMoonAbilitySystem : SharedHereticMoonSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly OldStatusEffectsSystem _statusOld = default!;
    [Dependency] private readonly StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedVisualBodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EventHereticMindGate>(OnMindGate);
        SubscribeLocalEvent<EventHereticLunarParade>(OnLunarParade);
        SubscribeLocalEvent<EventHereticRingleadersRise>(OnRingleader);
        SubscribeLocalEvent<EventHereticMoonlightEcho>(OnEcho);
        SubscribeLocalEvent<EventHereticTouchOfMadness>(OnTouchMadness);
        SubscribeLocalEvent<HereticAscensionMoonEvent>(OnAscension);
        SubscribeLocalEvent<MeleeHitEvent>(OnAmuletMelee);
    }

    private void OnAmuletMelee(MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count == 0)
            return;

        var held = _hands.GetActiveItem(args.User);
        if (held == null || !HasComp<MoonAmuletComponent>(held.Value))
            return;

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User)
                continue;
            if (HasComp<HereticComponent>(hit) || HasComp<GhoulComponent>(hit) || HasComp<MoonLunaticComponent>(hit))
                continue;
            ChannelAmulet(args.User, hit);
            args.Handled = true;
            break;
        }
    }

    private bool TryUse(Content.Shared.Actions.BaseActionEvent args)
    {
        if (args.Handled)
            return false;
        var ev = new BeforeCastSpellEvent(args.Performer);
        RaiseLocalEvent(args.Action, ref ev);
        if (ev.Cancelled)
            return false;
        args.Handled = true;
        return true;
    }

    private void OnMindGate(EventHereticMindGate args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        var target = args.Target;
        if (!HasComp<MobStateComponent>(target))
            return;
        if (!_heretic.TryGetHereticComponent(performer, out var heretic, out _))
            return;
        DealCellular(performer, args.SelfCellular, performer);
        EnsureSanity(target);
        var sanity = GetSanity(target);
        var duration = TimeSpan.FromSeconds(1f + (150f - sanity) / 150f * 14f);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Heretic/curse.ogg"), target);
        _statusNew.TryAddStatusEffectDuration(target, "MoonConfusedStatusEffect", duration);
        var oxy = new DamageSpecifier();
        oxy.DamageDict.Add("Asphyxiation", 30f);
        Damage.TryChangeDamage(target, oxy, true, false, origin: performer);
        var deaf = EnsureComp<ADTDeafenedComponent>(target);
        deaf.Severity = Math.Max(deaf.Severity, (float) duration.TotalSeconds * 0.5f);
        Dirty(target, deaf);
        if (TryComp(target, out OldStatusEffectsComponent? status))
        {
            _statusOld.TryAddStatusEffect(target, "Muted", duration, true, status);
            _statusNew.TryAddStatusEffectDuration(target, BlindnessSystem.BlindingStatusEffect, duration);
            _statusOld.TryAddStatusEffect(target, "Jitter", TimeSpan.FromSeconds(10), true, status);
            _statusOld.TryAddStatusEffect(target, "SlurredSpeech", duration, true, status);
        }
        DealCellular(target, args.TargetCellular, performer);
        AdjustSanity(target, -20f);
        if (sanity < 40f)
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);
        if (TryComp(performer, out MoonPassiveComponent? passive) && passive.AmuletEquipped)
            ChannelAmulet(performer, target);
    }

    private void OnLunarParade(EventHereticLunarParade args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        var from = _transform.GetMapCoordinates(performer);
        var to = args.Target;
        var dir = to.Position - from.Position;
        if (dir.Length() < 0.1f)
            dir = new System.Numerics.Vector2(1f, 0f);
        else
            dir = dir.Normalized();
        var proj = Spawn(args.Projectile, _transform.GetMapCoordinates(performer));
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Heretic/cosmic_energy.ogg"), performer);
        _gun.ShootProjectile(proj, dir, System.Numerics.Vector2.Zero, performer, performer, args.ProjectileSpeed);
    }

    private void OnRingleader(EventHereticRingleadersRise args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        var coords = Transform(performer).Coordinates;
        var performerName = Name(performer);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Heretic/moon_parade.ogg"), performer);
        var victims = _lookup.GetEntitiesInRange<MobStateComponent>(coords, args.Radius);
        foreach (var victim in victims)
        {
            if (victim.Owner == performer)
                continue;
            if (_mobState.IsDead(victim))
                continue;
            if (IsHereticOrLunatic(victim))
                continue;
            var illusion = Spawn(args.Illusion, _transform.GetMapCoordinates(victim));
            var illusionComp = EnsureComp<MoonIllusionComponent>(illusion);
            illusionComp.Caster = GetNetEntity(performer);
            Dirty(illusion, illusionComp);
            _meta.SetEntityName(illusion, performerName);
            if (TryComp<BodyComponent>(performer, out _) && TryComp<BodyComponent>(illusion, out _))
                _body.CopyAppearanceFrom(performer, illusion);
        }
    }

    private void OnEcho(EventHereticMoonlightEcho args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        if (!TryComp<MoonLunaticComponent>(performer, out var lunatic))
            return;
        var master = GetEntity(lunatic.Master);
        if (!Exists(master))
        {
            _popup.PopupEntity(Loc.GetString("heretic-moon-echo-lost"), performer, performer);
            return;
        }
        var ourCoords = _transform.GetMapCoordinates(performer);
        var theirCoords = _transform.GetMapCoordinates(master);
        var dist = (theirCoords.Position - ourCoords.Position).Length();
        string msg;
        if (dist < 15f)
            msg = Loc.GetString("heretic-moon-echo-near");
        else if (dist < 32f)
            msg = Loc.GetString("heretic-moon-echo-mid");
        else
            msg = Loc.GetString("heretic-moon-echo-far");
        if (ourCoords.MapId != theirCoords.MapId)
            msg = Loc.GetString("heretic-moon-echo-far");
        _popup.PopupEntity(msg, performer, performer);
    }

    private void OnTouchMadness(EventHereticTouchOfMadness args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        var target = args.Target;
        if (!HasComp<MobStateComponent>(target))
            return;
        if (IsHereticOrLunatic(target))
            return;
        AdjustSanity(target, -20f);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);
        SpawnMadnessFog(target);
    }

    private void SpawnMadnessFog(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        for (var i = 0; i < 4; i++)
        {
            var offset = _random.NextVector2(-1.5f, 1.5f);
            var pos = coords.Offset(offset).SnapToGrid(EntityManager);
            Spawn("HereticPaleFogMoon", pos);
        }
    }

    private void OnAscension(HereticAscensionMoonEvent args)
    {
        var heretic = args.Heretic;
        EnsureComp<MoonPassiveComponent>(heretic);
        EnsureComp<MoonAscensionAuraComponent>(heretic);
        var aura = Spawn("MoonAscensionAura", Transform(heretic).Coordinates);
        _transform.SetParent(aura, heretic);
        if (!_heretic.TryGetHereticComponent(heretic, out _, out var mind))
            return;
        var candidates = new List<Entity<MobStateComponent>>();
        var all = EntityQueryEnumerator<MobStateComponent>();
        while (all.MoveNext(out var uid, out var state))
        {
            if (uid == heretic)
                continue;
            if (_mobState.IsDead(uid))
                continue;
            if (state.CurrentState != MobState.Alive)
                continue;
            if (!HasComp<Content.Shared.Humanoid.HumanoidProfileComponent>(uid))
                continue;
            if (IsHereticOrLunatic(uid))
                continue;
            candidates.Add((uid, state));
        }
        var max = Math.Max(1, candidates.Count / 5);
        var count = 0;
        foreach (var cand in candidates)
        {
            if (count >= max)
                break;
            ConvertToLunatic(cand.Owner, heretic);
            count++;
        }
    }

    public void ConvertToLunatic(EntityUid target, EntityUid master)
    {
        _statusNew.TryAddStatusEffectDuration(target, "MoonConvertedStatusEffect", TimeSpan.FromHours(1));
        var lunatic = EnsureComp<MoonLunaticComponent>(target);
        lunatic.Master = GetNetEntity(master);
        Dirty(target, lunatic);
        EntityUid? echo = null;
        _actions.AddAction(target, ref echo, "ActionHereticMoonlightEcho");
        EntityUid? touch = null;
        _actions.AddAction(target, ref touch, "ActionHereticTouchOfMadness");
        Spawn("HereticMoonAmulet", Transform(target).Coordinates);
    }

    public void ChannelAmulet(EntityUid user, EntityUid target)
    {
        if (!HasComp<MobStateComponent>(target))
        {
            DealHeat(target, 30f, user);
            return;
        }
        if (IsHereticOrLunatic(target))
        {
            _popup.PopupEntity(Loc.GetString("heretic-amulet-resist"), user, user, PopupType.Medium);
            return;
        }
        if (HasComp<MoonConvertedComponent>(target) || HasComp<MoonSleptComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("heretic-amulet-damaging"), user, user, PopupType.Medium);
            DealCellular(target, 25f, user);
            return;
        }
        EnsureSanity(target);
        var sanity = GetSanity(target);
        if (sanity > 25f)
        {
            _popup.PopupEntity(Loc.GetString("heretic-amulet-too-strong"), user, user, PopupType.Medium);
            AdjustSanity(target, -20f);
            return;
        }
        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("heretic-amulet-mindshield"), user, user, PopupType.Medium);
            _statusNew.TryAddStatusEffectDuration(target, "MoonSleptStatusEffect", TimeSpan.FromSeconds(120));
            return;
        }
        _popup.PopupEntity(Loc.GetString("heretic-amulet-bends"), user, user, PopupType.Medium);
        ConvertToLunatic(target, user);
        var heal = new DamageSpecifier();
        heal.DamageDict.Add("Blunt", -50f);
        heal.DamageDict.Add("Slash", -50f);
        heal.DamageDict.Add("Heat", -50f);
        Damage.TryChangeDamage(target, heal, true, false, origin: user);
    }
}
