using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VendingMachines;

[RegisterComponent]
public sealed partial class ADTVendingFoodNutrimentReductionComponent : Component
{
    [DataField]
    public float NutrimentMultiplier = 0.5f;

    [DataField]
    public ProtoId<ReagentPrototype> NutrimentReagent = "Nutriment";
}