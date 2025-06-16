using Content.Client.SubFloor;
using Content.Shared.ADT.VentCrawling;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.ADT.VentCrawling;

public sealed class VentCrawlingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SubFloorHideSystem _subFloorHideSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalPlayer?.ControlledEntity;

        var ventCrawlerQuery = GetEntityQuery<VentCrawlerComponent>();

        if (player == null || !ventCrawlerQuery.TryGetComponent(player, out var playerVentCrawlerComponent))
        {
            _subFloorHideSystem.ShowVentPipe = false;
            return;
        }

        _subFloorHideSystem.ShowVentPipe = playerVentCrawlerComponent.InTube;
    }
}
