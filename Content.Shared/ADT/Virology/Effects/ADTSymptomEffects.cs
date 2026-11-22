using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Effects;

public sealed partial class ADTSymptomHeal : ADTSymptomEffect<ADTSymptomHeal>
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public float Base = 20f;

    [DataField]
    public float Scale = 1f;
}

public sealed partial class ADTSymptomCureDiseases : ADTSymptomEffect<ADTSymptomCureDiseases>
{
    [DataField]
    public LocId? CuredPopup;

    [DataField]
    public LocId? EndPopup;

    [DataField]
    public LocId? DepletedPopup;

    [DataField]
    public int Charges = 4;

    [DataField]
    public bool GiveImmunity;
}

public sealed partial class ADTSymptomLongevity : ADTSymptomEffect<ADTSymptomLongevity>
{
    [DataField]
    public int Longevity = 30;

    [DataField]
    public int Variation = 5;
}

public sealed partial class ADTSymptomAddReagent : ADTSymptomEffect<ADTSymptomAddReagent>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public float Amount = 1f;

    [DataField]
    public float MaxExisting = float.MaxValue;
}
