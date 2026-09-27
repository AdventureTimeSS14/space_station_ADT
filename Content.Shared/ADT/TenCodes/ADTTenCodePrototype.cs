using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TenCodes;

[Prototype("adtTenCode")]
public sealed partial class ADTTenCodePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Description;
}
