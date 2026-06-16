using Content.Client.ADT.Implants.Sandevistan;
using Content.Shared.ADT.Implants.KereznikovImplant;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Implants.KereznikovImplant;

public sealed class KereznikovVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private SandevistanTrailOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KereznikovActiveComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<KereznikovActiveComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<KereznikovActiveComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<KereznikovActiveComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new SandevistanTrailOverlay(EntityManager, _playerMan, _timing);
    }

    private void OnActiveInit(EntityUid uid, KereznikovActiveComponent comp, ComponentInit args)
    {
        if (uid == _playerMan.LocalEntity)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnActiveShutdown(EntityUid uid, KereznikovActiveComponent comp, ComponentShutdown args)
    {
        if (uid == _playerMan.LocalEntity)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, KereznikovActiveComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, KereznikovActiveComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
