using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Content.Server.StationEvents;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using System.Diagnostics;
using Content.Shared.GameTicking.Rules;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.GameTicking.Rules;

public sealed class ADTDynamicRuleSystem : GameRuleSystem<DynamicRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.Chaos = (int)_random.NextFloat(component.MinChaos, component.MaxChaos);

        if (_player.MaxPlayers > 0)
        {
            float playerRatio = (float)_player.PlayerCount / 60;
            component.Chaos = Math.Max(1, (int)(component.Chaos * playerRatio));
        }
        else
        {
            component.Chaos = Math.Max(1, component.Chaos);
        }

        _chatManager.SendAdminAnnouncement(Loc.GetString("dynamic-chaos-announcement", ("chaos", component.Chaos)));

        //тяжело, но тут идёт механизм выбора раундстарт антагов
        for (int i = 0; i < 1000 && component.Chaos >= 10; i++)
        {
            var rule = _random.Pick(component.RoundstartRules);
            if (_random.NextDouble() < rule.RerollChance)
                continue;
            if (component.Chaos - rule.Cost < 0)
            {
                foreach (var antag in component.RoundstartRules)
                {
                    if (component.Chaos - antag.Cost < 0)
                    {
                        component.Chaos -= antag.Cost;
                        component.AddedRules.Add(antag.Id);
                        break;
                    }
                }
                break;
            }
            else
            {
                component.Chaos -= rule.Cost;
                component.AddedRules.Add(rule.Id);
            }
        }

        //активация ивентов, не трогайте если не знаете
        foreach (var rule in component.AddedRules)
        {
            if (GameTicker.RunLevel <= GameRunLevel.InRound)
                GameTicker.AddGameRule(rule);
            else
                GameTicker.StartGameRule(rule, out _);
        }
    }

    public List<EntityUid> GetDynamicRules()
    {
        var rules = new List<EntityUid>();
        var query = EntityQueryEnumerator<DynamicRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp))
                continue;
            rules.Add(uid);
        }

        return rules;
    }

    public float? GetRuleBudget(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        UpdateBudget((entity.Owner, entity.Comp));
        return entity.Comp.Budget;
    }

    private void UpdateBudget(Entity<DynamicRuleComponent> entity)
    {
        var duration = (float) (Timing.CurTime - entity.Comp.LastBudgetUpdate).TotalSeconds;

        entity.Comp.Budget += duration * entity.Comp.BudgetPerSecond;
        entity.Comp.LastBudgetUpdate = Timing.CurTime;
    }

    public float? AdjustBudget(Entity<DynamicRuleComponent?> entity, float amount)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        UpdateBudget((entity.Owner, entity.Comp));
        entity.Comp.Budget += amount;
        return entity.Comp.Budget;
    }

    public float? SetBudget(Entity<DynamicRuleComponent?> entity, float amount)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        entity.Comp.LastBudgetUpdate = Timing.CurTime;
        entity.Comp.Budget = amount;
        return entity.Comp.Budget;
    }

    private IEnumerable<EntProtoId> GetRuleSpawns(Entity<DynamicRuleComponent> entity)
    {
        UpdateBudget((entity.Owner, entity.Comp));
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { HasBudgetCondition.BudgetContextKey, entity.Comp.Budget },
        });

        return _entityTable.GetSpawns(entity.Comp.Table, ctx: ctx);
    }

    public IEnumerable<EntProtoId> DryRun(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<EntProtoId>();

        return GetRuleSpawns((entity.Owner, entity.Comp));
    }

    /// <summary>
    /// Executes this rule, generating new dynamic rules and starting them.
    /// </summary>
    /// <returns>
    /// Returns a list of the rules that were executed.
    /// </returns>
    private List<EntityUid> Execute(Entity<DynamicRuleComponent> entity)
    {
        entity.Comp.NextRuleTime =
            Timing.CurTime + _random.Next(entity.Comp.MinRuleInterval, entity.Comp.MaxRuleInterval);

        var executedRules = new List<EntityUid>();

        foreach (var rule in GetRuleSpawns(entity))
        {
            var res = GameTicker.StartGameRule(rule, out var ruleUid);
            Debug.Assert(res);

            executedRules.Add(ruleUid);

            if (TryComp<DynamicRuleCostComponent>(ruleUid, out var cost))
            {
                entity.Comp.Budget -= cost.Cost;
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(entity)} ran rule {ToPrettyString(ruleUid)} with cost {cost.Cost} on budget {entity.Comp.Budget}.");
            }
            else
            {
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(entity)} ran rule {ToPrettyString(ruleUid)} which had no cost.");
            }
        }

        entity.Comp.Rules.AddRange(executedRules);
        return executedRules;
    }

    public IEnumerable<EntityUid> ExecuteNow(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<EntityUid>();

        return Execute((entity.Owner, entity.Comp));
    }

    public IEnumerable<EntityUid> Rules(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<EntityUid>();

        return entity.Comp.Rules;
    }

    protected override void Ended(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        //замена стандартных шкедуллеров

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

            if (scheduler.EventsBeforeAntag <= 0)
            {
                // var chaos = CheckChaos();
                // //проводит выбор по всем антагом, по схожести очкам хаоса, если не находит, выбирает случайного
                // foreach (var antag in scheduler.MidroundAntags)
                // {
                //     if (Math.Abs(antag.Cost - chaos) >= 15)
                //         continue;
                //     GameTicker.AddGameRule(antag.Id);
                //     scheduler.EventsBeforeAntag = scheduler.MaxEventsBeforeAntag;
                //     continue;
                // }
                foreach (var antag in scheduler.MidroundAntags)
                {
                    if (_random.NextDouble() < 0.9)
                        continue;
                    GameTicker.AddGameRule(antag.Id);
                    scheduler.EventsBeforeAntag = scheduler.MaxEventsBeforeAntag;
                }
            }

            scheduler.TimeUntilNextEvent = _random.NextFloat(scheduler.MinEventDelay, scheduler.MaxEventDelay);
            _event.RunRandomEvent(scheduler.ScheduledGameRules);
            scheduler.EventsBeforeAntag--;
        }
    }
    
    // public int CheckChaos()
    // {
    //     var chaos = 5; //изначально не нулевой для спавна мини-антагов
    //     var seccoms = EntityQueryEnumerator<MindShieldComponent, MobStateComponent>();
    //     //проверка живых людей с МЩ
    //     while (seccoms.MoveNext(out var uid, out var _, out var state))
    //     {
    //         if (state.CurrentState == MobState.Alive || state.CurrentState == MobState.Critical)
    //             chaos += 10;
    //     }
    //     //проверка на разгермы
    //     var alarms = EntityQueryEnumerator<AirAlarmComponent>();
    //     while (alarms.MoveNext(out var uid, out var alarm))
    //     {
    //         if (alarm.State != AtmosAlarmType.Normal && alarm.State != AtmosAlarmType.Invalid) // Исправлено: && вместо ||
    //             chaos--;
    //     }
    //     return chaos;
    // }
}