using Content.Shared.ADT.Abilities;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.ADT.Overlays;

public sealed partial class SandevistanVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private SandevistanVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanVisionComponent, ComponentInit>(OnSandevistanVisionInit);
        SubscribeLocalEvent<SandevistanVisionComponent, ComponentShutdown>(OnSandevistanVisionShutdown);
        SubscribeLocalEvent<SandevistanVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SandevistanVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnSandevistanVisionInit(EntityUid uid, SandevistanVisionComponent component, ComponentInit args)
    {
        if (uid == _playerMan.LocalEntity)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnSandevistanVisionShutdown(EntityUid uid, SandevistanVisionComponent component, ComponentShutdown args)
    {
        if (uid == _playerMan.LocalEntity)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, SandevistanVisionComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, SandevistanVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
            _overlayMan.RemoveOverlay(_overlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }
}
