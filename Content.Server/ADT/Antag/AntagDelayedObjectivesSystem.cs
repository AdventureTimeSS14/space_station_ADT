using Content.Server.ADT.Antag.Components;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Objectives;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Antag;

public sealed class AntagDelayedObjectivesSystem : EntitySystem
{
    public static readonly SoundSpecifier DefaultNotificationSound = new SoundCollectionSpecifier("ADTTraitorStart");

    private static readonly Color NotificationColor = Color.Red;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagDelayedObjectivesComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<AntagDelayedObjectivesComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind))
        {
            Log.Error($"Antag {ToPrettyString(args.EntityUid):player} was selected by {ToPrettyString(ent):rule} but had no mind attached!");
            return;
        }

        var roundDuration = _gameTicker.RoundDuration();
        if (roundDuration >= ent.Comp.Delay)
        {
            GiveObjectives(ent, mindId, mind);
            return;
        }

        var giveAt = ent.Comp.Delay;
        if (ent.Comp.MaxDelay is { } maxDelay)
            giveAt = _random.Next(ent.Comp.Delay, maxDelay);

        Timer.Spawn(giveAt - roundDuration, () =>
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
                return;

            if (!TryComp<AntagDelayedObjectivesComponent>(ent, out var comp))
                return;

            if (!TryComp<MindComponent>(mindId, out var targetMind))
                return;

            GiveObjectives(ent, mindId, targetMind);
        });
    }

    private void GiveObjectives(Entity<AntagDelayedObjectivesComponent> ent, EntityUid mindId, MindComponent mind)
    {
        var difficulty = 0f;
        var added = false;
        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && ent.Comp.MaxDifficulty > difficulty; pick++)
            {
                var remainingDifficulty = ent.Comp.MaxDifficulty - difficulty;
                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remainingDifficulty) is not { } objective)
                    continue;

                _mind.AddObjective(mindId, mind, objective);
                added = true;
                var adding = Comp<ObjectiveComponent>(objective).Difficulty;
                difficulty += adding;
                Log.Debug($"Added delayed objective {ToPrettyString(objective):objective} to {ToPrettyString(mindId):mind} with {adding} difficulty");
            }
        }

        if (added)
            NotifyObjectivesUpdated(mindId, ent.Comp.GreetSoundNotification);
    }

    public void NotifyObjectivesUpdated(EntityUid mindId, SoundSpecifier? sound = null)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;

        if (!_playerManager.TryGetSessionById(mind.UserId.Value, out var session))
            return;

        _audio.PlayGlobal(sound ?? DefaultNotificationSound, session);

        var message = Loc.GetString("objectives-updated");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, session.Channel, NotificationColor);
    }
}
