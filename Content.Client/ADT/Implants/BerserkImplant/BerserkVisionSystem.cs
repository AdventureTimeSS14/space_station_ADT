using Content.Shared.ADT.Implants.BerserkImplant;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.ADT.Implants.BerserkImplant;

public sealed class BerserkVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private BerserkVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<BerserkVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BerserkVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BerserkVisionComponent, LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<BerserkVisionComponent, LocalPlayerDetachedEvent>(OnDetach);
    }

    private void OnInit(EntityUid uid, BerserkVisionComponent comp, ComponentInit args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, BerserkVisionComponent comp, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnAttach(EntityUid uid, BerserkVisionComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnDetach(EntityUid uid, BerserkVisionComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
