using Content.Shared.ADT.Cyberpsychosis;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.ADT.Cyberpsychosis;

public sealed class CyberpsychosisOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private CyberpsychosisGlitchOverlay _overlay = default!;
    private BlackwallVisionOverlay _blackwallVision = default!;
    private BlackwallFlashOverlay _blackwallFlash = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveCyberpsychosisComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new();
        _blackwallVision = new();
        _blackwallFlash = new();
    }

    private void OnInit(EntityUid uid, ActiveCyberpsychosisComponent comp, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.AddOverlay(_overlay);
        _overlayMan.AddOverlay(_blackwallVision);
        _overlayMan.AddOverlay(_blackwallFlash);
    }

    private void OnShutdown(EntityUid uid, ActiveCyberpsychosisComponent comp, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlayMan.RemoveOverlay(_blackwallVision);
        _overlayMan.RemoveOverlay(_blackwallFlash);
    }

    private void OnAttached(EntityUid uid, ActiveCyberpsychosisComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
        _overlayMan.AddOverlay(_blackwallVision);
        _overlayMan.AddOverlay(_blackwallFlash);
    }

    private void OnDetached(EntityUid uid, ActiveCyberpsychosisComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlayMan.RemoveOverlay(_blackwallVision);
        _overlayMan.RemoveOverlay(_blackwallFlash);
    }
}
