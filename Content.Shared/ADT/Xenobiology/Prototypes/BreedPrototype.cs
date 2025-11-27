using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology;

[Prototype]
public sealed partial class BreedPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private init; } = null!;

    [DataField(required: true)]
    public string BreedName = string.Empty;

    [DataField]
    public EntProtoId ProducedExtract = "GreySlimeExtract";

    [DataField]
    public ComponentRegistry Components = new();
}
