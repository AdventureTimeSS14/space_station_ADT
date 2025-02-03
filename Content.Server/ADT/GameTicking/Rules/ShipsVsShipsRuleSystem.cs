using System.Diagnostics;
using Content.Server.ADT.ShipsVsShips;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Events;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.GameTicking.Rules;

public sealed class ShipsVsShipsRuleSystem : GameRuleSystem<ShipsVsShipsRuleComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipsVsShipsRuleComponent, RoundStartAttemptEvent>(OnStartAttempt); // Подписка на событие начала раунда.
        SubscribeLocalEvent<ShipsVsShipsRuleComponent, RoundEndTextAppendEvent>(OnRoundEndText); // Подписка на событие добавления текста при окончании раунда.

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded); // Подписка на событие взрыва ядерной бомбы.

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new [] { typeof(SpawnPointSystem) }); // Подписка на событие спавна игрока до системы точек спавна.
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete); // Подписка на событие завершения спавна игрока.

        SubscribeLocalEvent<ShipsVsShipsPlayerComponent, ComponentRemove>(OnComponentRemove); // Подписка на удаление компонента игрока.
        SubscribeLocalEvent<ShipsVsShipsPlayerComponent, MobStateChangedEvent>(OnMobStateChanged); // Подписка на изменение состояния моба.

        SubscribeLocalEvent<FTLCompletedEvent>(OnShuttleFTLCompleted); // Подписка на событие завершения FTL (сверхсветового) путешествия.
        SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnShuttleFTLAttempt); // Подписка на попытку FTL через консоль.
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt); // Подписка на попытку вызвать шаттл через консоль связи.
    }

    // Метод, вызываемый при добавлении правила игры.
    protected override void Added(EntityUid uid, ShipsVsShipsRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        // Установка времени следующей атаки FTL.
        component.AttackFtlTime = _timing.CurTime + component.AttackFtlDelay;

        // Получение всех карт и добавление их в компонент.
        var mapQuery = EntityQueryEnumerator<ShipsVsShipsMapComponent>();
        while (mapQuery.MoveNext(out var mapUid, out var map))
        {
            if (component.Maps.ContainsKey(map.Side)) // Проверка, содержит ли уже карта эту сторону.
                continue;

            component.Maps.Add(map.Side, mapUid); // Добавление карты в словарь.
        }

        // Получение всех шаттлов и добавление их в компонент.
        var shuttlesQuery = EntityQueryEnumerator<ShipsVsShipsShuttleComponent>();
        while (shuttlesQuery.MoveNext(out var shuttleUid, out var shuttle))
        {
            if (component.Shuttles.ContainsKey(shuttle.Side)) // Проверка, содержит ли уже шаттл эту сторону.
                continue;

            component.Shuttles.Add(shuttle.Side, shuttleUid); // Добавление шаттла в словарь.
        }
    }

    // Метод, вызываемый каждый тик игры для активных правил.
    protected override void ActiveTick(EntityUid uid, ShipsVsShipsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime); // Вызов базового метода.

        if (component.CanAttackFtl) // Проверка, можно ли атаковать FTL.
            return;

        if (component.AttackFtlTime < _timing.CurTime) // Проверка времени атаки FTL.
            return;

        _chat.DispatchGlobalAnnouncement(component.AttackFtlMessage, component.AttackFtlSender); // Отправка глобального сообщения о возможности атаки FTL.
        component.CanAttackFtl = true; // Установка флага, что атака FTL теперь возможна.
    }

    // Обработчик события начала раунда.
    private void OnStartAttempt(EntityUid uid, ShipsVsShipsRuleComponent component, RoundStartAttemptEvent ev)
    {
        // Здесь можно добавить логику для обработки начала раунда.
    }

    // Обработчик события добавления текста при окончании раунда.
    private void OnRoundEndText(EntityUid uid, ShipsVsShipsRuleComponent component, RoundEndTextAppendEvent ev)
    {
        var ruleQuery = QueryActiveRules(); // Запрос активных правил игры.
        while (ruleQuery.MoveNext(out _, out _, out var shipsVsShips, out _)) // Перебор активных правил "Ships vs Ships".
        {
            var winText = Loc.GetString($"ships-vs-ships-{shipsVsShips.WinSide.ToString().ToLower()}-{shipsVsShips.WinType.ToString().ToLower()}"); // Получение текста победы.
            ev.AddLine(winText); // Добавление текста победы в событие.

            foreach (var condition in shipsVsShips.WinConditions) // Перебор условий победы.
            {
                var text = Loc.GetString($"ships-vs-ships-condition-{condition.Side.ToString().ToLower()}-{condition.Condition.ToString().ToLower()}"); // Получение текста условия победы.
                ev.AddLine(text); // Добавление текста условия в событие.
            }
        }
    }

    // Обработчик события взрыва ядерной бомбы.
    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        // Запрос активных правил игры.
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out _, out var shipsVsShips, out _)) // Перебор всех активных правил "Ships vs Ships".
        {
            // Получение стороны, к которой принадлежит станция, вызвавшая событие.
            var side = GetEntitySide(ev.OwningStation);

            // Если сторона неизвестна, добавляем условие победы и устанавливаем тип победы как нейтральный.
            if (side == Side.Unknown)
            {
                AddWinCondition(side, WinCondition.NukeExplodedWrongPlace, shipsVsShips); // Условие: ядерная бомба взорвалась не в том месте.
                SetWinType(side, WinType.Neutral, shipsVsShips); // Тип победы: нейтральный.
                continue; // Переход к следующему правилу.
            }

            // Определяем победителя на основе стороны, которая вызвала событие.
            var winner = shipsVsShips.EnemySides[side]; // Получаем противника для текущей стороны.

            // Добавляем условие победы для противника и устанавливаем тип победы как основной.
            AddWinCondition(winner, WinCondition.NukeExploded, shipsVsShips); // Условие: ядерная бомба взорвалась у противника.
            SetWinType(winner, WinType.Major, shipsVsShips); // Тип победы: основной.
        }
    }

    // Обработчик события спавна игрока.
    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        ev.SpawnResult = null; // Устанавливаем результат спавна как null (не даем спавниться игроку).
    }

    // Обработчик события завершения спавна игрока.
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // Запрос активных правил игры.
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var shipsVsShips, out _)) // Перебор всех активных правил "Ships vs Ships".
        {
            // Проверяем, есть ли у сущности компонент игрока "ShipsVsShipsPlayerComponent".
            if (!TryComp<ShipsVsShipsPlayerComponent>(ev.Mob, out var player))
                continue; // Если компонента нет, переходим к следующему.

            var side = player.Side; // Получаем сторону игрока.
            Log.Debug($"Player {ev.Player} spawned on side {side}");

            // Если сторона игрока еще не зарегистрирована в правилах, добавляем её.
            if (!shipsVsShips.Players.ContainsKey(side))
                shipsVsShips.Players.Add(side, new HashSet<ICommonSession>()); // Создаем новый набор игроков для этой стороны.

            shipsVsShips.TotalAllPlayers++; // Увеличиваем общее количество игроков.
            shipsVsShips.Players[side].Add(ev.Player); // Добавляем игрока в соответствующий набор по его стороне.
            Log.Debug($"Player {ev.Player} added to {side} side.");
        }
    }

    // Обработчик события удаления компонента игрока.
    private void OnComponentRemove(Entity<ShipsVsShipsPlayerComponent> player, ref ComponentRemove args)
    {
        CheckPlayerCountCondition(); // Проверка условий по количеству игроков.
    }

    // Обработчик события изменения состояния моба (игрока).
    private void OnMobStateChanged(Entity<ShipsVsShipsPlayerComponent> player, ref MobStateChangedEvent args)
    {
        CheckPlayerCountCondition(); // Проверка условий по количеству игроков при изменении состояния.
    }

    // Обработчик события завершения FTL-путешествия шаттла.
    private void OnShuttleFTLCompleted(ref FTLCompletedEvent ev)
    {
        // Запрос активных правил игры.
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var shipsVsShips, out _)) // Перебор всех активных правил "Ships vs Ships".
        {
            // Проверяем, есть ли у сущности компонент шаттла "ShipsVsShipsShuttleComponent".
            if (!TryComp<ShipsVsShipsShuttleComponent>(ev.Entity, out var shuttleComponent))
                continue; // Если компонента нет, переходим к следующему.

            // Проверяем, зарегистрирован ли шаттл в правилах.
            if (!shipsVsShips.Shuttles.ContainsValue(ev.Entity))
                continue; // Если шаттл не зарегистрирован, переходим к следующему.

                // Если шаттл еще не атакует (не переместился для атаки).
            if (!shuttleComponent.FtlToAttack)
            {
                // Проверяем, переместился ли шаттл на карту противника.
                if (shipsVsShips.Maps[shipsVsShips.EnemySides[shuttleComponent.Side]] == ev.MapUid)
                {
                    // Если переместился на базу противника, устанавливаем флаг атаки FTL как true.
                    shuttleComponent.FtlToAttack = true;
                }

                continue; // Переход к следующему правилу.
            }

            // Проверяем, возвращается ли шаттл на карту, с которой он начал.
            if (shipsVsShips.Maps[shuttleComponent.Side] != ev.MapUid)
                continue; // Если не возвращается на свою карту, переходим к следующему.

            // Добавляем условие победы для стороны шаттла за успешное отступление и устанавливаем тип победы как минорный.
            AddWinCondition(shuttleComponent.Side, WinCondition.Retreat, shipsVsShips); // Условие: отступление шаттла.
            SetWinType(shipsVsShips.EnemySides[shuttleComponent.Side], WinType.Minor, shipsVsShips); // Тип победы: минорный для противника.
        }
    }

    // Обработчик события попытки FTL (сверхсветового) перемещения шаттла
    private void OnShuttleFTLAttempt(ref ConsoleFTLAttemptEvent ev)
    {
        // Запрашиваем активные правила игры
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var shipsVsShips, out _))
        {
            // Определяем сторону, к которой принадлежит сущность
            var side = GetEntitySide(ev.Uid);
            if (side == Side.Unknown) // Если сторона неизвестна, продолжаем цикл
                continue;
            if (shipsVsShips.CanAttackFtl) // Если разрешено атаковать FTL, продолжаем цикл
                continue;

            // Отменяем попытку FTL перемещения, если условия не выполнены
            ev.Cancelled = true;
        }
    }

    // Обработчик события попытки вызова шаттла через коммуникационную консоль
    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        // Запрашиваем активные правила игры
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var shipsVsShips, out _))
        {
            // Определяем сторону, к которой принадлежит сущность
            var side = GetEntitySide(ev.Uid);

            // Отменяем попытку вызова эвакуации, если это не разрешено
            if (!shipsVsShips.CanCallEmergency.Contains(side))
            {
                ev.Cancelled = true; // Отмена события
                continue; // Переход к следующей итерации цикла
            }

            // Добавляем условие победы для стороны
            AddWinCondition(side, WinCondition.CallEmergency, shipsVsShips);
            // Устанавливаем тип победы для противника
            SetWinType(shipsVsShips.EnemySides[side], WinType.Minor, shipsVsShips);
        }
    }

// Проверка условий по количеству игроков
    private void CheckPlayerCountCondition()
    {
        // Запрашиваем активные правила игры
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var shipsVsShips, out _))
        {
            List<Side> losesSides = new(); // Список сторон, которые проиграли
            foreach (var side in shipsVsShips.Players.Keys)
            {
                var sideCountTotal = shipsVsShips.Players[side].Count; // Общее количество игроков на стороне
                var sideCountAlive = 0; // Счетчик живых игроков на стороне

                foreach (var player in shipsVsShips.Players[side])
                {
                    if (player.AttachedEntity is not { } entity) continue; // Если нет прикрепленной сущности, пропускаем
                    if (_mobState.IsDead(entity)) continue; // Если игрок мертв, пропускаем
                    sideCountAlive++; // Увеличиваем счетчик живых игроков
                }

                var lossThreshold = sideCountTotal - sideCountTotal * shipsVsShips.MinDiedPercent; // Порог потерь

                // Если количество живых игроков больше порога, продолжаем цикл
                if (sideCountAlive > lossThreshold)
                    continue;

                losesSides.Add(side); // Добавляем сторону в список проигравших
            }

            // Если проигравших больше одной стороны, устанавливаем нейтральный результат
            if (losesSides.Count > 1)
            {
                SetWinType(Side.Unknown, WinType.Neutral, shipsVsShips);
                continue; // Переход к следующей итерации цикла
            }

            var looser = losesSides[0]; // Определяем проигравшую сторону

            // Добавляем условие победы для проигравшей стороны
            AddWinCondition(looser, WinCondition.MostDied, shipsVsShips);

            // Устанавливаем тип победы для противника
            SetWinType(shipsVsShips.EnemySides[looser], WinType.Major, shipsVsShips);
        }
    }

    // Получение стороны сущности по её UID
    private Side GetEntitySide(EntityUid? uid)
    {
        if (uid is null) // Если UID отсутствует, возвращаем неизвестную сторону
            return Side.Unknown;

        // Проверяем наличие компонентов у сущности и определяем её сторону
        if (TryComp<ShipsVsShipsShuttleComponent>(uid, out var selfShuttleComponent))
            return selfShuttleComponent.Side;

        if (TryComp<ShipsVsShipsMapComponent>(uid, out var selfMapComponent))
            return selfMapComponent.Side;

        var parent = Transform(uid.Value).ParentUid; // Получаем родительский UID

        if (TryComp<ShipsVsShipsShuttleComponent>(parent, out var shuttleComponent))
            return shuttleComponent.Side;

        if (TryComp<ShipsVsShipsMapComponent>(parent, out var mapComponent))
            return mapComponent.Side;

        return Side.Unknown; // Если сторона не определена, возвращаем неизвестную сторону
    }

    // Добавление условия победы для стороны в компоненте правил
    private void AddWinCondition(Side side, WinCondition condition, ShipsVsShipsRuleComponent component)
    {
        component.WinConditions.Add(new WindConditionInfo(side, condition)); // Добавляем новое условие победы
    }

    // Установка типа победы для стороны в компоненте правил и завершение раунда при необходимости
    private void SetWinType(Side side, WinType type, ShipsVsShipsRuleComponent component, bool endRound = true)
    {
        component.WinType = type; // Устанавливаем тип победы
        component.WinSide = side; // Устанавливаем сторону победителя

        if (!endRound) // Если не требуется завершение раунда, выходим из метода
            return;

        _roundEndSystem.EndRound(); // Завершаем раунд
    }
}
