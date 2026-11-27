namespace Content.Shared.ADT.EMP;

[RegisterComponent]
public sealed partial class EmpContainerProtectionComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, BatteryData> Batteries = new();

    [DataField]
    public string ContainerId = "cell_slot";

    [DataField]
    public float StrengthMultiplier = 0f;

    [DataField]
    public float DurationMultiplier = 1f;

    [DataField]
    public bool CapEnergyConsumption = false;

    [DataField]
    public bool CapRelativeToMaxCharge = true;

    [DataField]
    public float CapPercentage = 0.5f;

    public record struct BatteryData(float Protection, float DurationMult, bool CapConsumption, bool CapRelativeToMax, float CapPercentage);
}
