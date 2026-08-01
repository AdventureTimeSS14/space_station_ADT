namespace Content.Client.ADT.VisionOverlay;

public enum OverlayZIndexes
{
    NightVision = 10000,
    ThermalVisionEntityHighlight,
    /// <summary>
    /// Injects gas temperature as luminance into the framebuffer before the thermal LUT,
    /// so hot air/fire reads hot even when the tile is dark (no PointLight).
    /// </summary>
    ThermalVisionGas,
    ThermalVision,
}
