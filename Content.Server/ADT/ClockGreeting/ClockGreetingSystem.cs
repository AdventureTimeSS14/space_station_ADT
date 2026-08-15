using Content.Server.GameTicking;
using Content.Shared.ADT.ClockGreeting;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ClockGreeting;

public sealed class ClockGreetingSystem : EntitySystem
{
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
        if (ev.Silent || !_greeted.Add(ev.Player.UserId))
            return;

        var now = DateTime.UtcNow.AddYears(GameYearOffset).AddHours(EarthTimeOffsetHours);
        var shift = _timing.CurTime - _ticker.RoundStartTimeSpan;
        RaiseNetworkEvent(new ClockGreetingMessage(now, shift), ev.Player);
    }
}
