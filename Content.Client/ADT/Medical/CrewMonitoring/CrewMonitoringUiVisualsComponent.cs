namespace Content.Client.ADT.Medical.CrewMonitoring;

/// <summary>
/// Single-hue screen theme for the crew monitor UI inside the PDA bezel.
/// Relative brightness of panels/buttons is preserved; only the gamma/hue changes.
/// </summary>
[RegisterComponent]
public sealed partial class CrewMonitoringUiVisualsComponent : Component
{
    /// <summary>
    /// Source color for the whole inner UI palette (CRT, frames, chrome, map wash).
    /// </summary>
    [DataField]
    public string ThemeColor = "#6A7080";
}
