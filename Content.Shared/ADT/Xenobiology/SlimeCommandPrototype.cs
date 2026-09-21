using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology;
public enum SlimeCommandType : byte
{
    Greet,
    Follow,
    Stop,
    Attack,
}

[Prototype("slimeCommand")]
public sealed partial class SlimeCommandPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SlimeCommandType CommandType;

    [DataField(required: true)]
    public List<string> Keywords = [];
}
