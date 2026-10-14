using System.Numerics;
using Content.Client.ADT.Overlays.Shaders;
using Content.Shared.ADT.Clothing;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.ADT.Clothing;

/// <summary>
/// Красит обзор, пока надета одежда с <see cref="ADTVisionTintComponent"/>.
/// </summary>
public sealed class ADTVisionTintSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private ADTVisionTintOverlay _tint = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tint = new ADTVisionTintOverlay { Comp = new ADTVisionTintComponent() };
        _overlay.AddOverlay(_tint);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_tint);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!TryComp<ADTVisionTintComponent>(_player.LocalEntity, out var tint))
            return;

        _tint.TintColor = new Vector3(tint.Color.R, tint.Color.G, tint.Color.B);
        _tint.TintAmount = tint.Amount;
    }
}
