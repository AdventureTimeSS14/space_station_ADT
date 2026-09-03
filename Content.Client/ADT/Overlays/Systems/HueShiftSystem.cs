using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.ADT.Hallucinations.Components;

namespace Content.Client.ADT.Overlays;

public sealed partial class HueShiftSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private HueShiftOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HueShiftComponent, ComponentStartup>(OnMonochromacyStartup);
        SubscribeLocalEvent<HueShiftComponent, ComponentShutdown>(OnMonochromacyShutdown);

        SubscribeLocalEvent<HueShiftComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<HueShiftComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnMonochromacyStartup(EntityUid uid, HueShiftComponent component, ComponentStartup args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnMonochromacyShutdown(EntityUid uid, HueShiftComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnPlayerAttached(EntityUid uid, HueShiftComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, HueShiftComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
