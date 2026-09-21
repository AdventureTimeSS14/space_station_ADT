using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Server.ADT.Artillery;

[RegisterComponent]
public sealed partial class ArtilleryConsoleComponent : Component
{
    [DataField("uiUpdateInterval")]
    public float UiUpdateInterval = 1f;

    [ViewVariables]
    public float UiUpdateAccumulator;

    [ViewVariables]
    public NetEntity SelectedBeacon = NetEntity.Invalid;
}
