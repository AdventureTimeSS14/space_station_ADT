//

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent]
public sealed partial class LeechingWalkComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public FixedPoint2 BoneHeal = -5;

    [DataField]
    public DamageSpecifier ToHeal = new()
    {
        DamageDict =
        {
            {"Blunt", -1},
            {"Slash", -1},
            {"Piercing", -1},
            {"Heat", -1},
            {"Cold", -1},
            {"Shock", -1},
            {"Asphyxiation", -1},
            {"Bloodloss", -1},
            {"Caustic", -1},
            {"Poison", -1},
            {"Radiation", -1},
            {"Cellular", -1},
            {"Holy", -1},
        },
    };

    [DataField]
    public float StaminaHeal = 5f;

    [DataField]
    public float ChemPurgeRate = 3f;

    // ADT: reagent is named EssenceEldritch here
    [DataField]
    public ProtoId<ReagentPrototype> ExcludedReagent = "EssenceEldritch";

    [DataField]
    public FixedPoint2 BloodHeal = 5f;

    [DataField]
    public TimeSpan StunReduction = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public float TargetTemperature = 310f;
}
