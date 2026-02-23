using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition;

[Prototype]
public sealed partial class FlavorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("flavorType")]
    public FlavorType FlavorType { get; private set; } = FlavorType.Base;

    [DataField("description")]
    public string FlavorDescription { get; private set; } = default!;

    // ADT-Tweak-Start
    [DataField]
    public List<ProtoId<FlavorPrototype>> Neutralize { get; private set; } = new();
    // ADT-Tweak-End
}

public enum FlavorType : byte
{
    Base,
    Complex
}
