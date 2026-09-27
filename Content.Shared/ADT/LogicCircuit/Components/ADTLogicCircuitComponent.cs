using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.LogicCircuit.Components;

[RegisterComponent]
public sealed partial class ADTLogicCircuitComponent : Component
{
    [DataField]
    public LogicCircuitLayout Layout = new();

    [DataField]
    public int PowerBudget = 64;

    [DataField]
    public List<ProtoId<SinkPortPrototype>> InputPorts = new();

    [DataField]
    public List<ProtoId<SourcePortPrototype>> OutputPorts = new();

    [DataField]
    public bool Enabled = true;

    [ViewVariables]
    public CompiledLogicCircuit? Compiled;

    [ViewVariables]
    public LogicSignal[] PortInputs = Array.Empty<LogicSignal>();

    [ViewVariables]
    public LogicSignal[] PortOutputs = Array.Empty<LogicSignal>();

    [ViewVariables]
    public LogicSignal[] SentPortOutputs = Array.Empty<LogicSignal>();

    [ViewVariables]
    public ulong MomentaryPorts;

    [ViewVariables]
    public int IdleTicks;

    [ViewVariables]
    public TimeSpan LastTick;

    [ViewVariables]
    public bool UiOpen;

    [ViewVariables]
    public TimeSpan NextValueSend;

    [ViewVariables]
    public bool Broken;
}
