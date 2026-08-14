using Content.Server.GameTicking;
using Content.Shared.ADT.ClockGreeting;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ClockGreeting;

/// <summary>
/// Шлёт игроку приветствие с игровой датой и временем смены при первом спавне в раунд.
/// </summary>
public sealed class ClockGreetingSystem : EntitySystem
{
    // ADT-календарь: реальная дата +544 года, время МСК (как в принтере документов)
    private const int GameYearOffset = 544;
    private const int EarthTimeOffsetHours = 3;

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<NetUserId> _greeted = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _greeted.Clear();
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        // Только первый спавн в раунд, тихие (админские) спавны не трогаем
        if (ev.Silent || !_greeted.Add(ev.Player.UserId))
            return;

        var now = DateTime.UtcNow.AddYears(GameYearOffset).AddHours(EarthTimeOffsetHours);
        var shift = _timing.CurTime - _ticker.RoundStartTimeSpan;
        RaiseNetworkEvent(new ClockGreetingMessage(
            now.Year, now.Month, now.Day, now.Hour, now.Minute,
            (int) shift.TotalHours, shift.Minutes), ev.Player);
    }
}
