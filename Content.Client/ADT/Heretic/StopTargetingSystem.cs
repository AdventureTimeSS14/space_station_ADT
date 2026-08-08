using Content.Shared.ADT.Heretic;
using Robust.Client.Player;

namespace Content.Client.ADT.Heretic;

public sealed class StopTargetingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? StopTargeting;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StopTargetingEvent>(OnStopTargeting);
    }

    private void OnStopTargeting(StopTargetingEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession != _player.LocalSession)
            return;

        StopTargeting?.Invoke();
    }
}
