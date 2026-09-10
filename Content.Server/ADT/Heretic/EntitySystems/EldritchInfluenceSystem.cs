//

using System.Text.RegularExpressions;
using Content.Shared.ADT.Heretic;
using Content.Server.Chat.Managers;
using Content.Server.Heretic.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Heretic.EntitySystems;

public sealed class EldritchInfluenceSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effect = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EldritchInfluenceComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<EldritchInfluenceComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EldritchInfluenceComponent, EldritchInfluenceDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EldritchInfluenceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<EldritchInfluenceComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<SpectralComponent>(args.Examiner) || HasComp<GhostComponent>(args.Examiner) ||
            _heretic.IsHereticOrGhoul(args.Examiner))
            return;

        if (!_mind.TryGetMind(args.Examiner, out _, out var mind))
            return;

        if (!_playerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        _audio.PlayGlobal(ent.Comp.ExamineSound, session);

        var baseMessage = ent.Comp.ExamineBaseMessage;
        var message = Loc.GetString(_random.Pick(ent.Comp.HeathenExamineMessages));
        var size = ent.Comp.FontSize;
        var loc = Loc.GetString(baseMessage, ("size", size), ("text", message));
        _chatMan.ChatMessageToOne(ChatChannel.Server, message, loc, default, false, session.Channel);

        var effects = _random.Pick(ent.Comp.PossibleExamineEffects);
        foreach (var effect in effects)
        {
            _effect.TryApplyEffect(args.Examiner, effect);
        }
    }

    public bool CollectInfluence(Entity<EldritchInfluenceComponent> influence, EntityUid user, EntityUid? used = null)
    {
        // ADT: already drained/deleted, skip
        if (influence.Comp.Spent || TerminatingOrDeleted(influence))
            return false;

        var (time, hidden) = TryComp<EldritchInfluenceDrainerComponent>(used, out var drainer)
            ? (drainer.Time, drainer.Hidden)
            : (10f, true);

        var doAfter = new EldritchInfluenceDoAfterEvent();
        var dargs = new DoAfterArgs(EntityManager, user, time, doAfter, influence, influence, used)
        {
            NeedHand = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            MultiplyDelay = false,
            Hidden = hidden,
        };
        _popup.PopupEntity(Loc.GetString("heretic-influence-start"), influence, user);
        return _doafter.TryStartDoAfter(dargs);
    }

    private void OnInteract(Entity<EldritchInfluenceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !_heretic.TryGetHereticComponent(args.User, out _, out _))
            return;

        args.Handled = CollectInfluence(ent, args.User);
    }
    private void OnInteractUsing(Entity<EldritchInfluenceComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_heretic.TryGetHereticComponent(args.User, out _, out _))
            return;

        args.Handled = CollectInfluence(ent, args.User, args.Used);
    }
    private void OnDoAfter(Entity<EldritchInfluenceComponent> ent, ref EldritchInfluenceDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || !_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        // ADT: guard vs parallel do-afters double-firing
        if (ent.Comp.Spent || TerminatingOrDeleted(ent))
            return;

        ent.Comp.Spent = true;

        var knowledge = TryComp(args.Used, out EldritchInfluenceDrainerComponent? drainer)
            ? drainer.KnowledgePerInfluence
            : 1f;

        _heretic.UpdateKnowledge(args.User, knowledge);

        args.Handled = true;

        // ADT: grab coords/spawn before Del, or coords are lost
        var coords = Transform(args.Target.Value).Coordinates;
        Spawn("EldritchInfluenceIntermediate", coords);
        QueueDel(args.Target.Value);
    }
}
