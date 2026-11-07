using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Bones;

[Prototype("adtFractureType")]
public sealed partial class ADTFractureTypePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public int Power;

    [DataField]
    public float MinDamage;

    [DataField]
    public float DropChance;

    [DataField]
    public float SpreadDegrees;

    [DataField]
    public float DoAfterModifier;

    [DataField]
    public float FallChance;

    [DataField]
    public DamageSpecifier FallDamage = new();

    [DataField]
    public float SpeedModifier = 1f;

    [DataField]
    public bool CanSplint = true;

    [DataField]
    public bool Open;

    [DataField]
    public float ReattachChance;

    [DataField]
    public DamageSpecifier ReattachFailDamage = new();

    [DataField]
    public ProtoId<ADTFractureTypePrototype>? ReattachInto;

    [DataField]
    public float BleedAmount;

    [DataField]
    public float LockedDamage;
}
