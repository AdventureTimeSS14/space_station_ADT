using System.Numerics;
using Content.Client.ADT.Overlays.Shaders;
using Content.Shared.ADT.Bubblegum.Loot;
using Robust.Client.Graphics;

namespace Content.Client.ADT.Bubblegum;

public sealed class BloodFrenzyOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private ColorTintOverlay _tint = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tint = new ColorTintOverlay
        {
            TintColor = new Vector3(1f, 0f, 0f),
            TintAmount = 0.6f,
            Comp = new BloodFrenzyComponent(),
        };

        _overlay.AddOverlay(_tint);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_tint);
    }
}
