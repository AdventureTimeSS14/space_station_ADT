using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using Content.Server.GameTicking;
using Content.Shared.Atmos.Monitor;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.StationEvents;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;

namespace Content.Server.GameTicking.Rules;

public sealed class DynamicRuleSystem : GameRuleSystem<DynamicRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    private string _ruleCompName = default!;

    public override void Initialize()
    {
        base.Initialize();
        _ruleCompName = _compFact.GetComponentName(typeof(GameRuleComponent));
    }

    protected override void Added(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        component.Chaos = (uint)_random.NextFloat(component.MinChaos, component.MaxChaos);
        _adminLogger.Add(LogType.EventStarted, $"Current chaos level: {component.Chaos}");
        Log.Info($"Current chaos level: {component.Chaos}");

        while (component.Chaos >= 0)
        {
            var rule = _random.Pick(component.RoundstartRules);
            component.Chaos -= rule.Cost;
            if (component.Chaos < 0)
                break;
            component.AddedRules.Add(rule.Id);
        }

        foreach (var rule in component.AddedRules)
        {
            if (GameTicker.RunLevel <= GameRunLevel.InRound)
                GameTicker.AddGameRule(rule);
            else
                GameTicker.StartGameRule(rule, out _);
        }
    }

    protected override void Ended(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<DynamicRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (scheduler.TimeUntilNextEvent > 0f)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                continue;
            }

            scheduler.TimeUntilNextEvent = _random.NextFloat(scheduler.MinEventDelay, scheduler.MaxEventDelay);
            _event.RunRandomEvent(scheduler.ScheduledGameRules);
            if (scheduler.EventsBeforeAntag <= 0)
            {
                var chaos = CheckChaos();
                foreach (var antag in scheduler.MidroundAntags)
                {
                    if (Math.Abs(antag.Cost - chaos) >= 15)
                        return;
                    GameTicker.AddGameRule(antag.Id);
                    scheduler.EventsBeforeAntag = _random.Next(scheduler.MaxEventsBeforeAntag);
                }
            }
        }
    }
    public int CheckChaos()
    {
        var chaos = 5; //изначально не нулевой для спавна мини-антагов
        var seccoms = EntityQueryEnumerator<MindShieldComponent, MobStateComponent>();
        while (seccoms.MoveNext(out var uid, out var _, out var state))
        {
            if (state.CurrentState == MobState.Alive || state.CurrentState == MobState.Critical)
                chaos += 10;
        }
        var alarms = EntityQueryEnumerator<AirAlarmComponent>();
        while (alarms.MoveNext(out var uid, out var alarm))
        {
            if (alarm.State != AtmosAlarmType.Normal || alarm.State != AtmosAlarmType.Invalid)
                chaos--;
        }
        return chaos;
    }
}
