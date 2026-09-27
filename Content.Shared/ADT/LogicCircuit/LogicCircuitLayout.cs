using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.LogicCircuit;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class LogicCircuitLayout
{
    [DataField]
    public List<LogicNodeData> Nodes = new();

    [DataField]
    public List<LogicWireData> Wires = new();

    public LogicCircuitLayout Clone()
    {
        var result = new LogicCircuitLayout
        {
            Nodes = new List<LogicNodeData>(Nodes.Count),
            Wires = new List<LogicWireData>(Wires.Count),
        };

        foreach (var node in Nodes)
        {
            result.Nodes.Add(node.Clone());
        }

        foreach (var wire in Wires)
        {
            result.Wires.Add(wire.Clone());
        }

        return result;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class LogicNodeData
{
    /// <summary>
    /// Идентификатор ноды внутри схемы.
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public ProtoId<LogicElementPrototype> Proto;

    [DataField("pos")]
    public Vector2 Position;

    [DataField]
    public List<LogicSignal> Config = new();

    [DataField]
    public LogicSignal[] State = Array.Empty<LogicSignal>();

    public LogicNodeData Clone()
    {
        return new LogicNodeData
        {
            Id = Id,
            Proto = Proto,
            Position = Position,
            Config = new List<LogicSignal>(Config),
            State = State.Length == 0 ? Array.Empty<LogicSignal>() : (LogicSignal[]) State.Clone(),
        };
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class LogicWireData
{
    [DataField(required: true)]
    public string From = string.Empty;

    [DataField]
    public int FromPin;

    [DataField(required: true)]
    public string To = string.Empty;

    [DataField]
    public int ToPin;

    [DataField]
    public int Color;

    public LogicWireData Clone()
    {
        return new LogicWireData
        {
            From = From,
            FromPin = FromPin,
            To = To,
            ToPin = ToPin,
            Color = Color,
        };
    }
}
