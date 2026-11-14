using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.Prototypes;

[Prototype("crystallizerRecipe")]
public sealed partial class CrystallizerRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("minimumTemperature")]
    public float MinimumTemperature { get; private set; }

    [DataField("maximumTemperature")]
    public float MaximumTemperature { get; private set; }

    [DataField("minimumRequirements")]
    public Dictionary<Gas, float> MinimumRequirements { get; private set; } = new();

    [DataField("energyRelease")]
    public float EnergyRelease { get; private set; }

    [DataField("products")]
    public Dictionary<EntProtoId, int> Products { get; private set; } = new();
}
