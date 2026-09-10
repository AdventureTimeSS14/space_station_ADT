//

using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Eye;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio;
using Content.Server.Heretic.Components;
using Content.Server.Antag;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.ADT.CCVar;
using Content.Server.Objectives.Components;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Objectives;
using Content.Shared.Humanoid;
using Robust.Server.Player;
using Content.Server.Revolutionary.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Markings;
using Content.Server.Polymorph.Components;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles.Jobs;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.ADT.Heretic.Systems;
using Robust.Server.GameStates;
using Robust.Shared.Network;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Server.Hands.Systems;
using Content.Shared.ADT.Bed.Cryostorage;
using Robust.Shared.Enums;
using Content.Shared.Bed.Cryostorage;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem : SharedHereticSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly HereticRitualSystem _ritual = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _timer;
    private const float PassivePointCooldown = 20f * 60f;
    private bool _ascensionRequiresObjectives;

    private const int HereticVisFlags = (int) (VisibilityFlags.EldritchInfluence | VisibilityFlags.EldritchInfluenceSpent | VisibilityFlags.HereticCarving);

    private readonly Dictionary<EntityUid, List<(EntityUid Target, NetUserId User)>> _targetPvsOverrides = new();

    public static readonly ProtoId<NpcFactionPrototype> HereticFactionId = "Heretic";

    public static readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<HereticComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HereticComponent, EventHereticUpdateTargets>(OnUpdateTargets);
        SubscribeLocalEvent<HereticComponent, EventHereticRerollTargets>(OnRerollTargets);
        SubscribeLocalEvent<HereticComponent, EventHereticAscension>(OnAscension);

        SubscribeLocalEvent<HereticSacrificeTargetComponent, EntityTerminatingEvent>(OnTargetTerminating);
        SubscribeLocalEvent<HereticSacrificeTargetComponent, EntityEnteredCryostorageEvent>(OnTargetEnteredCryostorage);

        SubscribeLocalEvent<HereticComponent, MindGotRemovedEvent>(OnMindRemoved);
        SubscribeLocalEvent<HereticComponent, MindGotAddedEvent>(OnMindAdded);

        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisMask);

        SubscribeLocalEvent<HereticStartupEvent>(OnHereticStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        Subs.CVar(_cfg, ADTCCVars.HereticAscensionRequiresObjectives, value => _ascensionRequiresObjectives = value, true);

        _playerMan.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerMan.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        if (!_mind.TryGetMind(args.Session.UserId, out var mindId, out _) ||
            !TryComp(mindId, out HereticComponent? heretic))
            return;

        UpdateTargetPvsOverrides((mindId.Value, heretic));
    }

    private void OnMindAdded(Entity<HereticComponent> ent, ref MindGotAddedEvent args)
    {
        ent.Comp.MansusGraspAction = EntityUid.Invalid;

        if (TerminatingOrDeleted(args.Container))
            return;

        if (!HasComp<MobStateComponent>(args.Container))
        {
            // Don't kill stargazer if we got temporarily polymorphed
            if (TryComp(args.Container, out PolymorphedEntityComponent? p) &&
                (!p.Configuration.Forced || p.Configuration.Duration != null))
                return;

            var ev = new HereticMindDetachedEvent(ent);
            foreach (var minion in ent.Comp.Minions)
            {
                RaiseLocalEvent(minion, ref ev);
            }

            return;
        }

        SetMinionsMaster(ent, args.Container);
        RaiseKnowledgeEvents(ent, args.Container, false);
    }

    private void OnMindRemoved(Entity<HereticComponent> ent, ref MindGotRemovedEvent args)
    {
        ent.Comp.MansusGraspAction = EntityUid.Invalid;

        if (TerminatingOrDeleted(args.Container) || !HasComp<MobStateComponent>(args.Container))
            return;

        SetMinionsMaster(ent, null);
        RaiseKnowledgeEvents(ent, args.Container, true);
    }

    private void SetMinionsMaster(Entity<HereticComponent> ent, EntityUid? newMaster)
    {
        ent.Comp.Minions = ent.Comp.Minions.Where(Exists).ToHashSet();
        foreach (var uid in ent.Comp.Minions)
        {
            var minion = EnsureComp<HereticMinionComponent>(uid);
            minion.BoundHeretic = newMaster;
            Dirty(uid, minion);
        }
    }

    private void RaiseKnowledgeEvents(Entity<HereticComponent> mind, EntityUid body, bool negative)
    {
        foreach (var ev in mind.Comp.KnowledgeEvents)
        {
            RaiseKnowledgeEvent(body, ev, negative);
        }
    }

    private void OnHereticStartup(HereticStartupEvent ev)
    {
        foreach (var item in _hands.EnumerateHeld(ev.Heretic))
        {
            if (HasComp<MansusGraspComponent>(item))
                QueueDel(item);
        }

        if (ev.Negative)
            _npcFaction.RemoveFaction(ev.Heretic, HereticFactionId);
        else
        {
            _npcFaction.RemoveFaction(ev.Heretic, NanotrasenFactionId, false);
            _npcFaction.AddFaction(ev.Heretic, HereticFactionId);
        }

        if (!TryComp<EyeComponent>(ev.Heretic, out var eye))
            return;

        var mask = ev.Negative ? eye.VisibilityMask & ~HereticVisFlags : eye.VisibilityMask | HereticVisFlags;
        _eye.SetVisibilityMask(ev.Heretic, mask, eye);
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _timer = 0f;
        _targetPvsOverrides.Clear();
    }

    private List<EntityUid> ResolveSacrificeTargets(HereticComponent comp)
    {
        var targets = new List<EntityUid>();

        foreach (var target in comp.SacrificeTargets)
        {
            if (TryGetEntity(target.Entity, out var uid))
                targets.Add(uid.Value);
        }

        return targets;
    }

    private void UpdateSacrificeTargetMarkers(Entity<HereticComponent> ent, List<EntityUid> oldTargets, List<EntityUid> newTargets)
    {
        foreach (var old in oldTargets)
        {
            if (newTargets.Contains(old) || !TryComp(old, out HereticSacrificeTargetComponent? marker))
                continue;

            marker.Heretics.Remove(ent.Owner);

            if (marker.Heretics.Count == 0 && !TerminatingOrDeleted(old))
                RemComp<HereticSacrificeTargetComponent>(old);
        }

        foreach (var target in newTargets)
        {
            if (TerminatingOrDeleted(target))
                continue;

            EnsureComp<HereticSacrificeTargetComponent>(target).Heretics.Add(ent.Owner);
        }
    }

    private void UpdateTargetPvsOverrides(Entity<HereticComponent> ent)
    {
        if (_targetPvsOverrides.Remove(ent.Owner, out var oldOverrides))
        {
            foreach (var (target, user) in oldOverrides)
            {
                if (_playerMan.TryGetSessionById(user, out var oldSession))
                    _pvs.RemoveSessionOverride(target, oldSession);
            }
        }

        var newOverrides = new List<(EntityUid Target, NetUserId User)>();

        if (TryComp(ent, out MindComponent? mindComp) &&
            mindComp.UserId is { } userId &&
            _playerMan.TryGetSessionById(userId, out var session))
        {
            foreach (var target in ent.Comp.SacrificeTargets)
            {
                if (!TryGetEntity(target.Entity, out var tent) ||
                    !Exists(tent.Value) ||
                    EntityManager.IsQueuedForDeletion(tent.Value))
                    continue;

                newOverrides.Add((tent.Value, userId));
                _pvs.AddSessionOverride(tent.Value, session);
            }
        }

        _targetPvsOverrides[ent.Owner] = newOverrides;
    }

    private void ClearTargetPvsOverrides(EntityUid mindId)
    {
        if (!_targetPvsOverrides.Remove(mindId, out var oldOverrides))
            return;

        foreach (var (target, user) in oldOverrides)
        {
            if (_playerMan.TryGetSessionById(user, out var session))
                _pvs.RemoveSessionOverride(target, session);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;

        if (_timer < PassivePointCooldown)
            return;

        _timer = 0f;

        var query = EntityQueryEnumerator<HereticComponent, StoreComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var heretic, out var store, out var mind))
        {
            // passive point gain every 20 minutes
            UpdateMindKnowledge((uid, heretic, store, mind), null, 1f);
        }
    }

    public bool ObjectivesAllowAscension(Entity<HereticComponent, MindComponent?> ent)
    {
        if (!_ascensionRequiresObjectives)
            return true;

        if (!Resolve(ent, ref ent.Comp2))
            return false;

        Entity<MindComponent> mindEnt = (ent, ent.Comp2);

        foreach (var objId in ent.Comp1.AllObjectives)
        {
            if (_mind.TryFindObjective(mindEnt.AsNullable(), objId, out var obj) &&
                !_objectives.IsCompleted(obj.Value, mindEnt))
                return false;
        }

        return true;
    }

    public void UpdateMindKnowledge(Entity<HereticComponent, StoreComponent, MindComponent> ent,
        EntityUid? user,
        float amount,
        bool showText = true,
        bool playSound = true)
    {
        var (mindId, heretic, store, mind) = ent;
        var uid = user ?? mind.CurrentEntity;

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "KnowledgePoint", amount } }, mindId, store);
        _store.UpdateUserInterface(uid, mindId, store);

        if (_mind.TryGetObjectiveComp<HereticKnowledgeConditionComponent>(mindId, out var objective, mind))
            objective.Researched += amount;

        if (!showText && !playSound)
            return;

        if (!_playerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        if (playSound)
            _audio.PlayGlobal(heretic.InfluenceGainSound, session);

        if (!showText)
            return;

        var baseMessage = heretic.InfluenceGainBaseMessage;
        var message = Loc.GetString(_rand.Pick(heretic.InfluenceGainMessages));
        var size = heretic.InfluenceGainTextFontSize;
        var loc = Loc.GetString(baseMessage, ("size", size), ("text", message));
        // ADT: no UpdateFontSize/canCoalesce in our ChatManager
        _chatMan.ChatMessageToOne(ChatChannel.Server,
            message,
            loc,
            default,
            false,
            session.Channel);
    }

    public void UpdateKnowledge(EntityUid uid,
        float amount,
        bool showText = true,
        bool playSound = true,
        MindContainerComponent? mindContainer = null)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind, mindContainer) ||
            !TryComp(mindId, out StoreComponent? store) || !TryComp(mindId, out HereticComponent? heretic))
            return;

        UpdateMindKnowledge((mindId, heretic, store, mind), uid, amount, showText, playSound);
    }

    public HashSet<ProtoId<TagPrototype>>? TryGetRequiredKnowledgeTags(Entity<HereticComponent> ent)
    {
        if (ent.Comp.KnowledgeRequiredTags.Count > 0 || GenerateRequiredKnowledgeTags(ent))
            return ent.Comp.KnowledgeRequiredTags;

        return null;
    }

    public bool GenerateRequiredKnowledgeTags(Entity<HereticComponent> ent)
    {
        ent.Comp.KnowledgeRequiredTags.Clear();
        var dataset = _proto.Index(ent.Comp.KnowledgeDataset);
        for (var i = 0; i < 4; i++)
        {
            ent.Comp.KnowledgeRequiredTags.Add(_rand.Pick(dataset));
        }

        return ent.Comp.KnowledgeRequiredTags.Count > 0;
    }

    private void OnCompStartup(Entity<HereticComponent> ent, ref ComponentStartup args)
    {
        foreach (var k in ent.Comp.BaseKnowledge)
        {
            TryAddKnowledge(ent.AsNullable(), k);
        }

        GenerateRequiredKnowledgeTags(ent);

        RaiseLocalEvent(ent, new EventHereticRerollTargets());
    }

    private void OnShutdown(Entity<HereticComponent> ent, ref ComponentShutdown args)
    {
        ClearTargetPvsOverrides(ent.Owner);
        UpdateSacrificeTargetMarkers(ent, ResolveSacrificeTargets(ent.Comp), new List<EntityUid>());

        if (!TryComp(ent, out MindComponent? mind) || mind.CurrentEntity is not { } body || TerminatingOrDeleted(body))
            return;

        SetMinionsMaster(ent, null);
        RaiseKnowledgeEvents(ent, body, true);

        if (TerminatingOrDeleted(ent) || !TryComp(ent, out ActionsContainerComponent? container))
            return;

        foreach (var action in container.Container.ContainedEntities.ToList())
        {
            if (HasComp<HereticActionComponent>(action))
                _actionContainer.RemoveAction(action);
        }
    }

    private void OnGetVisMask(ref GetVisMaskEvent args)
    {
        if (!TryGetHereticComponent(args.Entity, out _, out _))
            return;

        args.VisibilityMask |= HereticVisFlags;
    }

    #region Internal events (target reroll, ascension, etc.)

    private void OnUpdateTargets(Entity<HereticComponent> ent, ref EventHereticUpdateTargets args)
    {
        RerollTargets(ent, true);
    }

    private void OnRerollTargets(Entity<HereticComponent> ent, ref EventHereticRerollTargets args)
    {
        RerollTargets(ent, false);
    }

    private void OnTargetTerminating(Entity<HereticSacrificeTargetComponent> ent, ref EntityTerminatingEvent args)
    {
        TargetLeftTheRound(ent);
    }

    private void OnTargetEnteredCryostorage(Entity<HereticSacrificeTargetComponent> ent, ref EntityEnteredCryostorageEvent args)
    {
        TargetLeftTheRound(ent);
    }

    private void TargetLeftTheRound(Entity<HereticSacrificeTargetComponent> ent)
    {
        // RerollTargets edits the marker's set, so walk a copy of it.
        foreach (var mindId in ent.Comp.Heretics.ToList())
        {
            if (!TryComp(mindId, out HereticComponent? heretic))
                continue;

            RerollTargets((mindId, heretic), true, ent.Owner);
        }

        ent.Comp.Heretics.Clear();
    }

    private void RerollTargets(Entity<HereticComponent> ent, bool keepValid, EntityUid? ignore = null)
    {
        // welcome to my linq smorgasbord of doom
        // have fun figuring that out

        var oldTargets = ResolveSacrificeTargets(ent.Comp);
        var candidates = _antag.GetAliveConnectedPlayers(_playerMan.Sessions)
            .Where(IsSessionValid)
            .Select(x => x.AttachedEntity!.Value)
            .Where(x => x != ignore && !TerminatingOrDeleted(x))
            .ToList();

        var pickedTargets = new List<EntityUid>();

        if (keepValid)
            pickedTargets = oldTargets.Where(candidates.Contains).ToList();

        var predicates = new List<Func<EntityUid, bool>>();

        // pick one command staff
        predicates.Add(HasComp<CommandStaffComponent>);
        // pick one security staff
        predicates.Add(HasComp<SecurityStaffComponent>);

        // add more predicates here

        foreach (var predicate in predicates)
        {
            if (pickedTargets.Any(predicate))
                continue;

            var list = candidates.Where(x => !pickedTargets.Contains(x) && predicate(x)).ToList();

            if (list.Count == 0)
                continue;

            pickedTargets.Add(_rand.Pick(list));
        }

        while (pickedTargets.Count < ent.Comp.MaxTargets)
        {
            var list = candidates.Where(x => !pickedTargets.Contains(x) &&
                                             !HasComp<CommandStaffComponent>(x) &&
                                             !HasComp<SecurityStaffComponent>(x)).ToList();

            if (list.Count == 0)
                list = candidates.Where(x => !pickedTargets.Contains(x)).ToList();

            if (list.Count == 0)
                break;

            pickedTargets.Add(_rand.PickAndTake(list));
        }

        // leave only unique entityuids
        pickedTargets = pickedTargets.Distinct().ToList();

        ent.Comp.SacrificeTargets = pickedTargets.Select(GetData).OfType<SacrificeTargetData>().ToList();
        Dirty(ent); // update client

        UpdateSacrificeTargetMarkers(ent, oldTargets, ResolveSacrificeTargets(ent.Comp));
        UpdateTargetPvsOverrides(ent);

        return;

        bool IsSessionValid(ICommonSession session)
        {
            if (!HasComp<HumanoidProfileComponent>(session.AttachedEntity))
                return false;

            if (HasComp<GhoulComponent>(session.AttachedEntity.Value))
                return false;

            if (HasComp<CryostorageContainedComponent>(session.AttachedEntity.Value))
                return false;

            if (!_mind.TryGetMind(session.AttachedEntity.Value, out var mind, out _) ||
                mind == ent.Owner || !_job.MindTryGetJobId(mind, out _))
                return false;

            return !HasComp<HereticComponent>(mind);
        }
    }

    private SacrificeTargetData? GetData(EntityUid uid)
    {
        if (!TryComp(uid, out HumanoidProfileComponent? humanoid))
            return null;

        if (!_mind.TryGetMind(uid, out var mind, out _) || !_job.MindTryGetJobId(mind, out var jobId) || jobId == null)
            return null;

        // ADT: no appearance cache, build like PolymorphSystem
        var profile = new HumanoidCharacterProfile().WithGender(humanoid.Gender)
            .WithSex(humanoid.Sex)
            .WithSpecies(humanoid.Species)
            .WithName(MetaData(uid).EntityName)
            .WithAge(humanoid.Age)
            .WithCharacterAppearance(HumanoidCharacterAppearance.DefaultWithSpecies(humanoid.Species, humanoid.Sex));

        var netEntity = GetNetEntity(uid);

        return new SacrificeTargetData { Entity = netEntity, Profile = profile, Job = jobId.Value };
    }

    // notify the crew of how good the person is and play the cool sound :godo:
    private void OnAscension(Entity<HereticComponent> ent, ref EventHereticAscension args)
    {
        if (!TryComp(ent, out MindComponent? mind) || mind.CurrentEntity is not { } uid)
            return;

        // you've already ascended, man.
        if (ent.Comp.Ascended || !ent.Comp.CanAscend)
            return;

        ent.Comp.Ascended = true;
        ent.Comp.KnownRituals.Remove("FeastOfOwls");
        ent.Comp.ChosenRitual = null;
        Dirty(ent);

        // how???
        if (ent.Comp.CurrentPath == null)
            return;

        if (TryComp(ent, out ActionsContainerComponent? container))
        {
            foreach (var action in container.Container.ContainedEntities)
            {
                if (TryComp(action, out ChangeUseDelayOnAscensionComponent? changeUseDelay) &&
                    (changeUseDelay.RequiredPath == null || changeUseDelay.RequiredPath == ent.Comp.CurrentPath))
                    _actions.SetUseDelay(action, changeUseDelay.NewUseDelay);
            }
        }

        var pathLoc = ent.Comp.CurrentPath.ToLower();
        var ascendSound =
            new SoundPathSpecifier($"/Audio/ADT/Heretic/Ambience/Antag/Heretic/ascend_{pathLoc}.ogg");
        _chat.DispatchGlobalAnnouncement(Loc.GetString($"heretic-ascension-{pathLoc}"),
            Name(uid),
            true,
            ascendSound,
            Color.Pink);
    }

    #endregion
}
