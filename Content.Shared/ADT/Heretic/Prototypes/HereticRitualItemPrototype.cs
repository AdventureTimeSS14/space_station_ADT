using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Prototypes;

[Prototype("hereticRitualItem")]
public sealed partial class HereticRitualItemPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField]
    public string Name = string.Empty;
}