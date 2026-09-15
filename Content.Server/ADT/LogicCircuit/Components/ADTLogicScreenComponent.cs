using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.LogicCircuit.Components;

[RegisterComponent]
public sealed partial class ADTLogicScreenComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> TextPort = "ADTLogicText";

    [DataField]
    public ProtoId<SinkPortPrototype> ColorPort = "ADTLogicColor";

    [DataField]
    public int MaxLength = 10;
}
