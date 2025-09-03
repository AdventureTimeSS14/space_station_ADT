using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour.Components;

/// <summary>
/// Component for storing user preferences for the devour system
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DevourSettingsComponent : Component
{
    /// <summary>
    /// Whether scat is enabled for this player
    /// </summary>
    [DataField("scatEnabled")]
    public bool ScatEnabled = true;

    /// <summary>
    /// Whether to show digestion messages
    /// </summary>
    [DataField("showDigestionMessages")]
    public bool ShowDigestionMessages = true;

    /// <summary>
    /// Whether to show devour messages
    /// </summary>
    [DataField("showDevourMessages")]
    public bool ShowDevourMessages = true;

    /// <summary>
    /// Whether to show scat messages
    /// </summary>
    [DataField("showScatMessages")]
    public bool ShowScatMessages = true;

    /// <summary>
    /// Whether to show detailed digestion information
    /// </summary>
    [DataField("showDetailedDigestion")]
    public bool ShowDetailedDigestion = false;
}
