using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Botany.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTPlantTrayUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WaterCapacityMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float NutritionCapacityMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float NutrientConsumptionMultiplier = 1f;

    [DataField]
    public float? BaseMaxWaterLevel;

    [DataField]
    public float? BaseMaxNutritionLevel;

    [DataField]
    public TimeSpan? CycleDelay;
}
