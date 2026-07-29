using Content.Client.ADT.VisionOverlay;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

public sealed class ThermalVisionEntityHighlightOverlay : BaseEntityHighlightOverlay
{
    public ThermalVisionEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.ThermalVisionEntityHighlight;
    }
}
