using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BarbellBench;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarbellBenchVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string AttachedState = "barbell-overlay-up";

    [DataField, AutoNetworkedField]
    public string OverlayBaseState = "barbell-overlay-up";

    [DataField, AutoNetworkedField]
    public string OverlayRepFlickState = "barbell-up";
}

