using Content.Shared.ADT.Glitch;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.ADT.Glitch;

public sealed class GlitchSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private GlitchOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlitchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GlitchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GlitchComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<GlitchComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new();
    }

    private void OnInit(EntityUid uid, GlitchComponent comp, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, GlitchComponent comp, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnAttached(EntityUid uid, GlitchComponent comp, LocalPlayerAttachedEvent args)
        => _overlayMan.AddOverlay(_overlay);

    private void OnDetached(EntityUid uid, GlitchComponent comp, LocalPlayerDetachedEvent args)
        => _overlayMan.RemoveOverlay(_overlay);
}
