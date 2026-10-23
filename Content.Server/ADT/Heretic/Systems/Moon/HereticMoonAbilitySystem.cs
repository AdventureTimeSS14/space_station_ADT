using Content.Server.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ADT.Deafness;
using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic.Moon;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Heretic.Systems.Moon;
using Content.Shared.Body;
using Content.Shared.Clumsy;
using Content.Shared.Body.Components;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
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
using Robust.Shared.Timing;
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
    [Dependency] private readonly SharedMoonHallucinationSystem _hallucinations = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
        if (target == performer)
            return;
        if (IsHereticOrLunatic(target))
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
        _statusNew.TrySetStatusEffectDuration(target, "StatusEffectMuted", duration);
        _statusNew.TryAddStatusEffectDuration(target, BlindnessSystem.BlindingStatusEffect, duration);
        if (TryComp(target, out OldStatusEffectsComponent? status))
        {
            _statusOld.TryAddStatusEffect(target, "Jitter", TimeSpan.FromSeconds(10), true, status);
            _statusOld.TryAddStatusEffect(target, "SlurredSpeech", duration, true, status);
        }
        DealCellular(target, args.TargetCellular, performer);
        AdjustSanity(target, -20f);
        _hallucinations.CauseHallucinations(target, 2, 15f, true);
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
        var to = _transform.ToMapCoordinates(args.Target);
        if (to.MapId != from.MapId)
            return;
        var dir = to.Position - from.Position;
        if (dir.Length() < 0.1f)
            dir = new System.Numerics.Vector2(1f, 0f);
        else
            dir = dir.Normalized();
        var spawnCoords = new Robust.Shared.Map.MapCoordinates(from.Position + dir * 0.6f, from.MapId);
        var proj = Spawn(args.Projectile, spawnCoords);
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
            _meta.SetEntityDescription(illusion, MetaData(performer).EntityDescription);
            if (TryComp<BodyComponent>(performer, out _) && TryComp<BodyComponent>(illusion, out _))
                _body.CopyAppearanceFrom(performer, illusion);
        }
    }

    private void OnEcho(EventHereticMoonlightEcho args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        EntityUid? target = null;
        if (TryComp<MoonLunaticComponent>(performer, out var lunatic))
        {
            target = GetEntity(lunatic.Master);
        }
        else if (HasComp<HereticComponent>(performer))
        {
            var ourCoords = _transform.GetMapCoordinates(performer);
            float nearestDist = float.MaxValue;
            var query = EntityQueryEnumerator<MoonLunaticComponent, TransformComponent>();
            while (query.MoveNext(out var lUid, out var lComp, out var lXform))
            {
                if (GetEntity(lComp.Master) != performer)
                    continue;
                var coords = _transform.GetMapCoordinates(lUid, lXform);
                if (coords.MapId != ourCoords.MapId)
                    continue;
                var d = (coords.Position - ourCoords.Position).Length();
                if (d < nearestDist)
                {
                    nearestDist = d;
                    target = lUid;
                }
            }
        }

        if (target == null || !Exists(target.Value))
        {
            _popup.PopupEntity(Loc.GetString("heretic-moon-echo-lost"), performer, performer);
            return;
        }
        var ourCoords2 = _transform.GetMapCoordinates(performer);
        var theirCoords = _transform.GetMapCoordinates(target.Value);
        string dist;
        if (ourCoords2.MapId != theirCoords.MapId)
        {
            _popup.PopupEntity(Loc.GetString("heretic-moon-echo-far"), performer, performer);
            return;
        }
        var delta = theirCoords.Position - ourCoords2.Position;
        var distLen = delta.Length();
        if (distLen < 15f)
            dist = Loc.GetString("heretic-moon-echo-near");
        else if (distLen < 32f)
            dist = Loc.GetString("heretic-moon-echo-mid");
        else
            dist = Loc.GetString("heretic-moon-echo-far");
        var angle = Math.Atan2(delta.Y, delta.X) * 180.0 / Math.PI;
        var dir = angle switch
        {
            >= -22.5 and < 22.5 => Loc.GetString("heretic-moon-echo-dir-e"),
            >= 22.5 and < 67.5 => Loc.GetString("heretic-moon-echo-dir-ne"),
            >= 67.5 and < 112.5 => Loc.GetString("heretic-moon-echo-dir-n"),
            >= 112.5 and < 157.5 => Loc.GetString("heretic-moon-echo-dir-nw"),
            >= 157.5 or < -157.5 => Loc.GetString("heretic-moon-echo-dir-w"),
            >= -157.5 and < -112.5 => Loc.GetString("heretic-moon-echo-dir-sw"),
            >= -112.5 and < -67.5 => Loc.GetString("heretic-moon-echo-dir-s"),
            _ => Loc.GetString("heretic-moon-echo-dir-se"),
        };
        _popup.PopupEntity(Loc.GetString("heretic-moon-echo-full", ("dir", dir), ("dist", dist)), performer, performer);
    }

    private void OnTouchMadness(EventHereticTouchOfMadness args)
    {
        if (!TryUse(args))
            return;
        var performer = args.Performer;
        var target = args.Target;
        if (!HasComp<MobStateComponent>(target))
            return;
        if (target == performer)
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
        RechargeMoonSpells(master);
        _hallucinations.CauseHallucinations(target, 3, 20f, true);
    }

    private void RechargeMoonSpells(EntityUid heretic)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActionComponent>();
        while (query.MoveNext(out var uid, out var action))
        {
            if (action.AttachedEntity != heretic)
                continue;
            if (action.Cooldown == null)
                continue;
            var proto = MetaData(uid).EntityPrototype?.ID;
            if (proto != "ActionHereticMindGate" && proto != "ActionHereticLunarParade" && proto != "ActionHereticRingleadersRise")
                continue;
            var remaining = action.Cooldown.Value.End - now;
            if (remaining <= TimeSpan.Zero)
                continue;
            var total = action.Cooldown.Value.End - action.Cooldown.Value.Start;
            var reduced = remaining - total * 0.25;
            Entity<Content.Shared.Actions.Components.ActionComponent?> act = (uid, action);
            if (reduced <= TimeSpan.Zero)
                _actions.ClearCooldown(act);
            else
                _actions.SetCooldown(act, action.Cooldown.Value.Start, now + reduced);
        }
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
