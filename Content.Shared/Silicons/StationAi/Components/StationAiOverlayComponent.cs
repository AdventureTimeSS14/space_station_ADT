using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Handles the static overlay for station AI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiOverlayComponent : Component
// ADT Tweak start
{
    [DataField, AutoNetworkedField]
    public string? VisionNetwork;
}
// ADT Tweak end
