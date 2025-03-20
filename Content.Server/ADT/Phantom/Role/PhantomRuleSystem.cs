using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.ADT.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.ADT.Phantom;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind;
using Content.Shared.GameTicking.Components;
using Content.Shared.Stunnable;
using Content.Shared.Damage;

namespace Content.Server.GameTicking.Rules;

public sealed class PhantomRuleSystem : GameRuleSystem<PhantomRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("Phantom");

        SubscribeLocalEvent<PhantomRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        SubscribeLocalEvent<PhantomComponent, PhantomDiedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PhantomComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<PhantomComponent, PhantomLevelReachedEvent>(OnNewLevelReached);

        SubscribeLocalEvent<PhantomTyranyTargetComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

        SubscribeLocalEvent<PhantomComponent, PhantomTyranyEvent>(OnTyranyAttempt);
        SubscribeLocalEvent<PhantomComponent, PhantomNightmareEvent>(OnNightmareAttempt);
        SubscribeLocalEvent<PhantomComponent, PhantomReincarnatedEvent>(OnReincarnation);
    }

    protected override void Started(EntityUid uid, PhantomRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, PhantomRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        if (!comp.PhantomMind.HasValue)
            return;
        // just temporary until this is deleted
        args.Minds.Add((comp.PhantomMind.Value.Owner, comp.PhantomMind.Value.Comp.CharacterName ?? "?"));
        args.AgentName = "Phantom";
    }

    protected override void AppendRoundEndText(EntityUid uid, PhantomRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            foreach (var cond in phantom.WinConditions)
            {
                if (cond == PhantomWinCondition.TyranyAttemped)
                {
                    SetWinType(phantom.Owner, PhantomWinType.PhantomMajor);
                    phantom.WinConditions.Add(PhantomWinCondition.TyranySuccess);
                }
                if (phantom.PhantomMind.HasValue && phantom.PhantomMind.Value.Comp.OwnedEntity != null)
                {
                    if (HasComp<PhantomComponent>(phantom.PhantomMind.Value.Comp.OwnedEntity) || HasComp<PhantomPuppetComponent>(phantom.PhantomMind.Value.Comp.OwnedEntity))
                        phantom.WinConditions.Add(PhantomWinCondition.PhantomAlive);
                    if (HasComp<PhantomPuppetComponent>(phantom.PhantomMind.Value.Comp.OwnedEntity))
                        SetWinType(phantom.Owner, PhantomWinType.PhantomMajor);
                }
            }

            var winText = Loc.GetString($"phantom-{phantom.WinType.ToString().ToLower()}");
            args.AddLine(winText);

            foreach (var cond in phantom.WinConditions)
            {
                var text = Loc.GetString($"phantom-cond-{cond.ToString().ToLower()}");
                args.AddLine(text);
            }
        }
    }

    private void OnMobStateChanged(EntityUid uid, PhantomComponent component, ref PhantomDiedEvent args)
    {
        foreach (var vessel in component.Vessels)
        {
            var stunTime = TimeSpan.FromSeconds(4);
            RemComp<VesselComponent>(uid);
            _sharedStun.TryParalyze(vessel, stunTime, true);
        }
        foreach (var cursed in component.CursedVessels)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", 200);
            damage.DamageDict.Add("Cellular", 200);
            _damage.TryChangeDamage(cursed, damage, true);
            RemComp<PhantomPuppetComponent>(uid);
        }
        if (HasComp<PhantomHolderComponent>(component.Holder))
            RemCompDeferred<PhantomHolderComponent>(component.Holder);

        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            if (component.FinalAbilityUsed)
                phantom.WinType = PhantomWinType.CrewMinor;
            else
                phantom.WinType = PhantomWinType.CrewMajor;
        }
    }

    private void OnTyranyAttempt(EntityUid uid, PhantomComponent component, ref PhantomTyranyEvent args)
    {
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            phantom.WinConditions.Add(PhantomWinCondition.TyranyAttemped);
            SetWinType(phantom.Owner, PhantomWinType.PhantomMinor);
        }
    }

    private void OnNightmareAttempt(EntityUid uid, PhantomComponent component, ref PhantomNightmareEvent args)
    {
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            phantom.WinConditions.Add(PhantomWinCondition.NightmareStarted);
            SetWinType(phantom.Owner, PhantomWinType.PhantomMajor);
        }
    }

    private void OnReincarnation(EntityUid uid, PhantomComponent component, ref PhantomReincarnatedEvent args)
    {
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            phantom.WinConditions.Add(PhantomWinCondition.PhantomReincarnated);
            SetWinType(phantom.Owner, PhantomWinType.PhantomMinor);
        }
    }

    private void OnMindAdded(EntityUid uid, PhantomComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind) || mind.Session == null)
            return;

        // if (_roles.MindHasRole<PhantomRoleComponent>(mindId))
        //     return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var phantom, out _))
        {
            if (phantom.PhantomMind.HasValue)
                continue;

            var finObjective = _objectives.GetRandomObjective(mindId, mind, phantom.FinalObjectiveGroup, 10f);
            if (finObjective == null)
                break;

            _mind.AddObjective(mindId, mind, finObjective.Value);
            phantom.PhantomMind = (mindId, mind);
            _antagSelection.SendBriefing(mind.Session, Loc.GetString("phantom-welcome"), Color.BlueViolet, component.GreetSoundNotification);
            break;
        }
    }

    private void OnNewLevelReached(EntityUid uid, PhantomComponent component, ref PhantomLevelReachedEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind) || mind.Session == null)
            return;
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var phantom, out _))
        {
            if (phantom.PhantomMind != (mindId, mind))
                continue;
            var objective = _objectives.GetRandomObjective(mindId, mind, phantom.ObjectiveGroup, _cfg.GetCVar(ADTCCVars.PhantomMaxDifficulty));
            if (objective == null)
                continue;

            _mind.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            Log.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
        }
    }

    private void SetWinType(EntityUid uid, PhantomWinType type, PhantomRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.WinType = type;
    }

    private void OnCommandMobStateChanged(EntityUid uid, PhantomTyranyTargetComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose();
    }

    private bool CheckCommandLose()
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }

        return IsGroupDead(commandList, true);
    }

    private bool IsGroupDead(List<EntityUid> list, bool checkOffStation)
    {
        var dead = 0;
        foreach (var entity in list)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                else if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    dead++;
                }
            }
            //If they don't have the MobStateComponent they might as well be dead.
            else
            {
                dead++;
            }
        }

        return dead == list.Count || list.Count == 0;
    }

    private void AddObjectives(EntityUid mindId, MindComponent mind, PhantomRuleComponent component)
    {
        var maxDifficulty = _cfg.GetCVar(ADTCCVars.PhantomMaxDifficulty);
        var maxPicks = _cfg.GetCVar(ADTCCVars.PhantomMaxPicks);
        var difficulty = 0f;
        Log.Debug($"Attempting {maxPicks} objective picks with {maxDifficulty} difficulty");
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, component.ObjectiveGroup, _cfg.GetCVar(ADTCCVars.PhantomMaxDifficulty));
            if (objective == null)
                continue;

            _mind.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            difficulty += adding;
            Log.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
        }
        var finObjective = _objectives.GetRandomObjective(mindId, mind, component.FinalObjectiveGroup, 10f);
        if (finObjective == null)
            return;

        _mind.AddObjective(mindId, mind, finObjective.Value);

    }
    private sealed class PhantomSpawn
    {
        public ICommonSession? Session { get; private set; }
        public PhantomSpawnPreset Type { get; private set; }

        public PhantomSpawn(ICommonSession? session, PhantomSpawnPreset type)
        {
            Session = session;
            Type = type;
        }
    }
}
