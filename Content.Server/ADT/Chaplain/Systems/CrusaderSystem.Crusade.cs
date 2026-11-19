using Content.Server.Chat.Managers;
using Content.Server.Roles.Jobs;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Damage.Systems;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed partial class CrusaderSystem
{
    [Dependency] private GroupHealSystem _groupHeal = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly SoundSpecifier CrusadeBeginSound = new SoundPathSpecifier("/Audio/ADT/Chaplain/holy_shield.ogg");
    private static readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/ambiholy.ogg");

    private bool _crusadeActive;

    private void InitializeCrusade()
    {
        SubscribeLocalEvent<CrusaderComponent, EntParentChangedMessage>(OnCrusaderParentChanged);
        SubscribeLocalEvent<CrusaderComponent, MobStateChangedEvent>(OnCrusaderMobStateChanged);
        SubscribeLocalEvent<CrusaderComponent, ComponentShutdown>(OnCrusaderShutdown);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded, after: new[] { typeof(JobSystem) });
        SubscribeLocalEvent<HolyAffectedComponent, ComponentStartup>(OnHolyAffectedStartup);
        SubscribeLocalEvent<UnholyFervorComponent, ComponentShutdown>(OnFervorShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_crusadeActive)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<UnholyFervorComponent>();
        while (query.MoveNext(out var uid, out var fervor))
        {
            if (now < fervor.NextRegen)
                continue;

            fervor.NextRegen = now + fervor.RegenDelay;

            if (!_mobState.IsDead(uid))
                _groupHeal.Heal(uid, null, fervor.Regen);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _crusadeActive = false;
    }

    private void OnCrusaderParentChanged(Entity<CrusaderComponent> ent, ref EntParentChangedMessage args)
    {
        UpdateCrusade(null);
    }

    private void OnCrusaderMobStateChanged(Entity<CrusaderComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateCrusade(null);
    }

    private void OnCrusaderShutdown(Entity<CrusaderComponent> ent, ref ComponentShutdown args)
    {
        UpdateCrusade(ent);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } body || !TryComp<CrusaderComponent>(body, out var crusader) || crusader.Briefed)
            return;

        if (!_jobs.MindTryGetJob(args.MindId, out _) || !_player.TryGetSessionById(args.Mind.UserId, out var session))
            return;

        crusader.Briefed = true;
        var message = Loc.GetString(crusader.Master ? "crusader-briefing-master" : "crusader-briefing-follower");
        var wrapped = Loc.GetString("chaplain-briefing-wrapped", ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, session.Channel, Color.Gold);
        _audio.PlayGlobal(BriefingSound, session);
    }

    private void OnHolyAffectedStartup(Entity<HolyAffectedComponent> ent, ref ComponentStartup args)
    {
        if (_crusadeActive)
            Empower(ent, false);
    }

    private void OnFervorShutdown(Entity<UnholyFervorComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (action, delay) in ent.Comp.OriginalDelays)
        {
            if (Exists(action))
                _actions.SetUseDelay(new Entity<ActionComponent?>(action, null), delay);
        }

        ent.Comp.OriginalDelays.Clear();
    }

    private void UpdateCrusade(EntityUid? removed)
    {
        var active = false;
        var query = EntityQueryEnumerator<CrusaderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (uid == removed || _mobState.IsDead(uid))
                continue;

            if (xform.GridUid is not { } grid || !HasComp<StationMemberComponent>(grid))
                continue;

            active = true;
            break;
        }

        if (active == _crusadeActive)
            return;

        _crusadeActive = active;

        var holy = EntityQueryEnumerator<HolyAffectedComponent>();
        while (holy.MoveNext(out var uid, out _))
        {
            if (active)
                Empower(uid, true);
            else if (RemComp<UnholyFervorComponent>(uid))
                Notify(uid, "crusade-ends-antag");
        }
    }

    private void Empower(EntityUid uid, bool announce)
    {
        if (HasComp<UnholyFervorComponent>(uid))
            return;

        var fervor = AddComp<UnholyFervorComponent>(uid);
        fervor.NextRegen = _timing.CurTime + fervor.RegenDelay;

        foreach (var action in _actions.GetActions(uid))
        {
            if (action.Comp.UseDelay is not { } delay)
                continue;

            fervor.OriginalDelays[action.Owner] = delay;
            _actions.SetUseDelay(action.AsNullable(), delay * fervor.ActionDelayMultiplier);
        }

        if (announce)
        {
            Notify(uid, "crusade-begins-antag");
            _audio.PlayEntity(CrusadeBeginSound, uid, uid);
        }
    }

    private void Notify(EntityUid uid, string loc)
    {
        var message = Loc.GetString(loc);
        _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var wrapped = Loc.GetString("crusade-antag-wrapped", ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, actor.PlayerSession.Channel, Color.DarkRed);
    }
}
