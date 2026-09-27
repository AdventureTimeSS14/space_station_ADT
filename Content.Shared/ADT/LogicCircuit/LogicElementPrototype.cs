using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.LogicCircuit;

[Serializable, NetSerializable]
public enum LogicElementCategory : byte
{
    Special = 0,
    Logic,
    Arithmetic,
    Comparison,
    Memory,
    Signal,
    Text,
}

[Serializable, NetSerializable]
public enum LogicConfigFieldType : byte
{
    Number,
    Text,
    Boolean,
    DevicePort,
}

[DataDefinition]
public sealed partial class LogicElementConfigField
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LogicConfigFieldType Type = LogicConfigFieldType.Number;

    [DataField]
    public string Default = string.Empty;

    [DataField]
    public float Min = float.MinValue;

    [DataField]
    public float Max = float.MaxValue;
}

[Prototype("logicElement")]
public sealed partial class LogicElementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField]
    public LogicElementCategory Category = LogicElementCategory.Special;

    [DataField]
    public int Cost = 1;

    [DataField]
    public Color Color = Color.FromHex("#3f4c5a");

    [DataField]
    public List<LocId> Inputs = new();

    [DataField]
    public List<LocId> Outputs = new();

    [DataField]
    public int StateSize;

    [DataField]
    public List<LogicElementConfigField> Config = new();

    [DataField]
    public bool Hidden;

    [DataField(required: true)]
    public LogicElementBehavior Behavior = default!;
}
