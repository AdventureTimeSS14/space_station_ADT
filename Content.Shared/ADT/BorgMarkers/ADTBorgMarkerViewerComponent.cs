using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BorgMarkers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTBorgMarkerViewerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ScreenPadding = 46f;

    [DataField, AutoNetworkedField]
    public float ArrowSize = 15f;
}
