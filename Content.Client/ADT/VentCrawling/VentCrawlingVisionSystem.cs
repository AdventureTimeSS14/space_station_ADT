using Content.Client.SubFloor;
using Content.Shared.ADT.VentCrawling;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.ADT.VentCrawling;

public sealed class VentCrawlingSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SubFloorHideSystem _subFloorHideSystem = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;

    private VentCrawPipeOverlay _pipeOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _pipeOverlay = new();
        _overlayManager.AddOverlay(_pipeOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_pipeOverlay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalSession?.AttachedEntity;

        var ventCrawlerQuery = GetEntityQuery<VentCrawlerComponent>();

        if (player == null || !ventCrawlerQuery.TryGetComponent(player, out var playerVentCrawlerComponent))
        {
            _subFloorHideSystem.ShowVentPipe = false;
            return;
        }

        _subFloorHideSystem.ShowVentPipe = playerVentCrawlerComponent.InTube;
    }
}
