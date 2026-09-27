using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTDryableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public float DryingTemperature = 500f;

    [DataField]
    public int Wetness = 30;

    [ViewVariables]
    public int Remaining;
}
