using Content.Shared.ADT.Implants.SecondHeartImplant;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.ADT.Implants.SecondHeartImplant;

public sealed class SecondHeartVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private SecondHeartVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<SecondHeartVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SecondHeartVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SecondHeartVisionComponent, LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<SecondHeartVisionComponent, LocalPlayerDetachedEvent>(OnDetach);
    }

    private void OnInit(EntityUid uid, SecondHeartVisionComponent comp, ComponentInit args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, SecondHeartVisionComponent comp, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnAttach(EntityUid uid, SecondHeartVisionComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnDetach(EntityUid uid, SecondHeartVisionComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
