using Content.Client.ADT.VisionOverlay;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

public sealed class ThermalVisionOverlay : BaseVisionOverlay
{
    public ThermalVisionOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.ThermalVision;
    }
}
