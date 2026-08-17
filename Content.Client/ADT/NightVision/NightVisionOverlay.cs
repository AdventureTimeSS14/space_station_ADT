using Content.Client.ADT.VisionOverlay;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.NightVision;

public sealed class NightVisionOverlay : BaseVisionOverlay
{
    public NightVisionOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.NightVision;
    }
}
