using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTMagmiteScannerViewerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 30f;
}
