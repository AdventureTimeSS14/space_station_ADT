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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveCyberpsychosisComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ActiveCyberpsychosisComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new();
    }

    private void OnInit(EntityUid uid, ActiveCyberpsychosisComponent comp, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, ActiveCyberpsychosisComponent comp, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnAttached(EntityUid uid, ActiveCyberpsychosisComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnDetached(EntityUid uid, ActiveCyberpsychosisComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
