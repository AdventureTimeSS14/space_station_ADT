using Content.Shared.ADT.Construction;

namespace Content.Server.ADT.Power.Generator;

[RegisterComponent, Access(typeof(GeneratorMachinePartsSystem))]
public sealed partial class GeneratorMachinePartsComponent : Component
{
    [DataField]
    public float OutputMultiplier = 1f;

    [DataField]
    public float ConsumptionMultiplier = 1f;
}