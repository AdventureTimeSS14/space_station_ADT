using System.Numerics;
using Content.Client.ADT.VisionOverlay;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.NightVision;

public sealed class NightVisionOverlay : BaseVisionOverlay
{
    public NightVisionOverlay(ShaderPrototype shader, Color? tint) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.NightVision;

        if (tint is { } color)
            _shader.SetParameter("TintColor", new Vector3(color.R, color.G, color.B));
    }
}
