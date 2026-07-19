using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.Chat;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BubblegumComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BubblegumComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<BubblegumComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<BubblegumComponent, ProjectileHitAttemptEvent>(OnProjectileHitAttempt);
        SubscribeLocalEvent<BubblegumComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<BubblegumComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<BubblegumComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<BubblegumComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        Say(ent, "bubblegum-death", 3, true);
        var query = EntityQueryEnumerator<BubblegumMinionComponent>();
        while (query.MoveNext(out var minion, out var comp))
        {
            if (comp.Summoner == null || comp.Summoner == ent.Owner)
                QueueDel(minion);
        }
    }

    private void OnStartup(Entity<BubblegumComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnMapInit(Entity<BubblegumComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actions.AddAction(ent.Owner, action);
        }
    }

    private void OnGetVisMask(Entity<BubblegumComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Bubblegum;
    }

    private void OnMove(Entity<BubblegumComponent> ent, ref MoveEvent args)
    {
        if (args.ParentChanged)
            return;

        if (args.OldPosition == args.NewPosition)
            return;

        if (HasComp<BubblegumActiveChargeComponent>(ent))
            return;

        if (_mobState.IsDead(ent))
            return;

        if (_timing.CurTime - ent.Comp.LastStepSound >= ent.Comp.StepSoundCooldown)
        {
            ent.Comp.LastStepSound = _timing.CurTime;
            _audio.PlayPvs(ent.Comp.StepSound, ent);
        }

        if (!HasComp<ActorComponent>(ent) && !HasAiTarget(ent))
            return;

        var map = _transform.ToMapCoordinates(args.NewPosition);
        if (map.MapId == MapId.Nullspace)
            return;

        if (ent.Comp.LastBloodPosition is { } last
            && last.MapId == map.MapId
            && (last.Position - map.Position).LengthSquared() < ent.Comp.BloodStepDistance * ent.Comp.BloodStepDistance)
            return;

        ent.Comp.LastBloodPosition = map;
        SpawnAtCoords(ent.Comp.BloodPrototype, args.NewPosition);
    }

    private bool HasAiTarget(EntityUid uid)
    {
        return TryComp<HTNComponent>(uid, out var htn)
               && htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager)
               && !TerminatingOrDeleted(target);
    }

    private void OnDamageDealt(Entity<BubblegumComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= FixedPoint2.Zero)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            var maxHealth = 0f;
            foreach (var (damage, _) in thresholds.Thresholds)
            {
                if ((float)damage > maxHealth)
                    maxHealth = (float)damage;
            }

            var damageTaken = (float)_damageable.GetTotalDamage(ent.Owner);
            ent.Comp.AngerModifier = Math.Clamp(damageTaken / 60f, 0f, 20f);

            if (!ent.Comp.InSmashPhase && damageTaken >= maxHealth * 0.5f)
            {
                ent.Comp.InSmashPhase = true;
                Say(ent, "bubblegum-smash-phase", 3, true);
            }
        }

        if (_random.Prob(ent.Comp.ThickBloodOnDamageChance))
        {
            SpawnAtCoords(ent.Comp.ThickBloodPrototype, _transform.GetMoverCoordinates(ent));
        }
    }

    private void OnProjectileHitAttempt(Entity<BubblegumComponent> ent, ref ProjectileHitAttemptEvent args)
    {
        if (!IsEnraged(ent))
            return;

        args.Cancel();
        _audio.PlayPvs(ent.Comp.RangedDeflectSound, ent);
    }

    private void OnRefreshSpeed(Entity<BubblegumComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!IsEnraged(ent))
            return;

        args.ModifySpeed(ent.Comp.EnragedSpeedMultiplier, ent.Comp.EnragedSpeedMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BubblegumComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EnrageEndsAt == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < comp.EnrageEndsAt)
                continue;

            ExitEnrage((uid, comp));
        }
    }

    public bool IsEnraged(Entity<BubblegumComponent> ent)
    {
        return ent.Comp.EnrageEndsAt > _timing.CurTime;
    }

    public bool CanEnrage(Entity<BubblegumComponent> ent)
    {
        return _timing.CurTime >= ent.Comp.NextEnrageAvailableAt;
    }

    public bool TryEnterEnrage(Entity<BubblegumComponent> ent)
    {
        if (!CanEnrage(ent))
            return false;

        var duration = TimeSpan.FromSeconds(ent.Comp.BaseEnrageDuration.TotalSeconds *
                                            Math.Clamp(ent.Comp.AngerModifier / 20f, 0.5f, 1f));
        ent.Comp.EnrageEndsAt = _timing.CurTime + duration;
        ent.Comp.NextEnrageAvailableAt = _timing.CurTime + duration * 2;

        if (ent.Comp.HealingReceivedFromEnrage < ent.Comp.MaxHealingFromEnrage && ent.Comp.EnrageHeal > 0)
        {
            var heal = Math.Min(ent.Comp.EnrageHeal, ent.Comp.MaxHealingFromEnrage - ent.Comp.HealingReceivedFromEnrage);
            ent.Comp.HealingReceivedFromEnrage += heal;
            var healing = new DamageSpecifier();
            healing.DamageDict.Add("Blunt", -heal);
            _damageable.TryChangeDamage(ent.Owner, healing, true);
        }

        Say(ent, "bubblegum-enrage", 3);
        _appearance.SetData(ent, BubblegumVisuals.Enraged, true);
        _speed.RefreshMovementSpeedModifiers(ent);

        Dirty(ent);
        return true;
    }

    private void ExitEnrage(Entity<BubblegumComponent> ent)
    {
        ent.Comp.EnrageEndsAt = TimeSpan.Zero;
        _appearance.SetData(ent, BubblegumVisuals.Enraged, false);
        _speed.RefreshMovementSpeedModifiers(ent);
        Dirty(ent);
    }

    public EntityUid? SpawnAtCoords(string proto, EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return null;

        if (HasSameProtoOnTile(proto, coords))
            return null;

        return SpawnAtPosition(proto, coords);
    }

    public EntityUid? SpawnAtCoords(string proto, MapCoordinates coords)
    {
        if (coords.MapId == MapId.Nullspace)
            return null;

        if (HasSameProtoOnTile(proto, _transform.ToCoordinates(coords)))
            return null;

        return Spawn(proto, coords);
    }

    private bool HasSameProtoOnTile(string proto, EntityCoordinates coords)
    {
        foreach (var uid in _lookup.GetEntitiesIntersecting(coords.SnapToGrid(EntityManager)))
        {
            if (MetaData(uid).EntityPrototype?.ID == proto)
                return true;
        }
        return false;
    }

    public void Say(EntityUid uid, string locId, int count, bool ignore = false)
    {
        if (!TryComp<BubblegumComponent>(uid, out var bubblegum))
            return;

        var now = _timing.CurTime;
        if (!ignore && now < bubblegum.NextSpeechTime)
            return;

        if (!ignore && _random.NextFloat() > 0.3f)
            return;

        if (!TryComp<MetaDataComponent>(uid, out var meta))
            return;

        bubblegum.NextSpeechTime = now + TimeSpan.FromSeconds(15);

        var name = FormattedMessage.EscapeText(meta.EntityName);
        var message = Loc.GetString($"{locId}-{_random.Next(1, count + 1)}");

        var wrappedMessage = Loc.GetString("chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString("chat-speech-verb-default")),
            ("fontType", "Blackcraft"),
            ("fontSize", 16),
            ("defaultFont", "NotoSans"),
            ("defaultSize", 12),
            ("message", message));

        _chat.SendInVoiceRange(
            ChatChannel.Local,
            message,
            wrappedMessage,
            wrappedMessage,
            uid,
            ChatTransmitRange.HideChat);

        var minionQuery = EntityQueryEnumerator<BubblegumMinionComponent, MetaDataComponent>();
        while (minionQuery.MoveNext(out var minionUid, out var minionComp, out var minionMeta))
        {
            if (minionComp.Summoner != uid)
                continue;

            var minionName = FormattedMessage.EscapeText(minionMeta.EntityName);
            var minionWrapped = Loc.GetString("chat-manager-entity-say-wrap-message",
                ("entityName", minionName),
                ("verb", Loc.GetString("chat-speech-verb-default")),
                ("fontType", "Blackcraft"),
                ("fontSize", 16),
                ("defaultFont", "NotoSans"),
                ("defaultSize", 12),
                ("message", message));

            _chat.SendInVoiceRange(
                ChatChannel.Local,
                message,
                minionWrapped,
                minionWrapped,
                minionUid,
                ChatTransmitRange.HideChat);
        }
    }
}
