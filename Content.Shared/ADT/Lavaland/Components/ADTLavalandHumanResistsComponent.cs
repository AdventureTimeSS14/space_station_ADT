using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTLavalandHumanResistsComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Reference;

    [ViewVariables]
    public bool Active;

    [ViewVariables]
    public ProtoId<DamageModifierSetPrototype>? OriginalModifierSet;

    [ViewVariables]
    public float OriginalHeatThreshold;

    [ViewVariables]
    public float OriginalColdThreshold;

    [ViewVariables]
    public DamageSpecifier? OriginalHeatDamage;

    [ViewVariables]
    public DamageSpecifier? OriginalColdDamage;
}
