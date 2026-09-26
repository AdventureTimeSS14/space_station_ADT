using Content.Shared.ADT.LogicCircuit;

namespace Content.Server.ADT.LogicCircuit.Components;

[RegisterComponent]
public sealed partial class ADTLogicDiskComponent : Component
{
    [DataField]
    public LogicCircuitLayout? Layout;

    [DataField]
    public string Label = string.Empty;
}
