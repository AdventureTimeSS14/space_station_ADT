using Content.Shared.ADT.RemoteEye.Components;
using Content.Shared.ADT.RemoteEye.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;


namespace Content.Client.ADT.RemoteEye;

public sealed class RemoteEyeOverlaySystem : SharedRemoteEyeSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private RemoteEyeOverlay? _remoteEyeOverlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoteEyeOverlayComponent, LocalPlayerAttachedEvent>(OnOverlayInit);
        SubscribeLocalEvent<RemoteEyeOverlayComponent, LocalPlayerDetachedEvent>(OnOverlayShutdown);
    }

    private void AddOverlay()
    {
        if (_remoteEyeOverlay != null)
            return;

        _remoteEyeOverlay = new RemoteEyeOverlay();
        _overlay.AddOverlay(_remoteEyeOverlay);
    }

    private void RemoveOverlay()
    {
        if (_remoteEyeOverlay == null)
            return;

        _overlay.RemoveOverlay(_remoteEyeOverlay);
        _remoteEyeOverlay = null;
    }

    private void OnOverlayInit(Entity<RemoteEyeOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != ent.Owner)
            return;

        AddOverlay();
    }

    private void OnOverlayShutdown(Entity<RemoteEyeOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }
}
