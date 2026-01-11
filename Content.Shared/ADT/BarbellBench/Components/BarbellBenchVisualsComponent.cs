using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BarbellBench;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarbellBenchVisualsComponent : Component
{
    /// <summary>
    /// RSI state for the overlay entity (base/idle).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string OverlayBaseState = "barbell-overlay-up";

    /// <summary>
    /// RSI state to flick when performing rep (overlay entity).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string OverlayRepFlickState = "barbell-up";
}

