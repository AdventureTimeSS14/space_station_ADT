using Content.Shared.ADT.ImplantActivationVision;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.ADT.ImplantActivationVision;

public sealed class ImplantActivationVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private ImplantActivationVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<ImplantActivationVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ImplantActivationVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImplantActivationVisionComponent, LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<ImplantActivationVisionComponent, LocalPlayerDetachedEvent>(OnDetach);
    }

    private void OnInit(EntityUid uid, ImplantActivationVisionComponent comp, ComponentInit args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, ImplantActivationVisionComponent comp, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnAttach(EntityUid uid, ImplantActivationVisionComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnDetach(EntityUid uid, ImplantActivationVisionComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
