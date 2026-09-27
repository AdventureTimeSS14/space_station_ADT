using Content.Shared.ADT.Mining.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.Mining;

public sealed class ADTMagmiteMiningOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private ADTMagmiteMiningOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMagmiteScannerViewerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTMagmiteScannerViewerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTMagmiteScannerViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ADTMagmiteScannerViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<ADTMagmiteScannerViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<ADTMagmiteScannerViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(Entity<ADTMagmiteScannerViewerComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent.Owner)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(Entity<ADTMagmiteScannerViewerComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            _overlayMan.RemoveOverlay(_overlay);
    }
}
