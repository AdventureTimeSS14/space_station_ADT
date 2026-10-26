using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BorgMarkers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTBorgMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color MarkerColor = Color.Cyan;
}
