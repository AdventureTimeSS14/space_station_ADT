using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Content.Server.StationEvents;
using Content.Server.Database;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class DynamicRuleSystem : GameRuleSystem<DynamicRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
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