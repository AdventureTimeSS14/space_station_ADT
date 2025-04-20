using Content.Shared.ADT.MesonVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.MesonVision;

public sealed class MesonVisionSystem : SharedMesonVisionSystem
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MesonVisionComponent, LocalPlayerAttachedEvent>(OnMesonVisionAttached);
        SubscribeLocalEvent<MesonVisionComponent, LocalPlayerDetachedEvent>(OnMesonVisionDetached);
    }

    private void OnMesonVisionAttached(Entity<MesonVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        MesonVisionChanged(ent);
    }

    private void OnMesonVisionDetached(Entity<MesonVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    protected override void MesonVisionChanged(Entity<MesonVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        switch (ent.Comp.State)
        {
            case MesonVisionState.Off:
                Off();
                break;
            case MesonVisionState.Full:
                Full(ent);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void MesonVisionRemoved(Entity<MesonVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay(new MesonVisionOverlay());
    }

    private void Full(Entity<MesonVisionComponent> ent)
    {
        _overlay.AddOverlay(new MesonVisionOverlay());
    }
}
