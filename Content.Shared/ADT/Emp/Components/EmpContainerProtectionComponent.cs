using Content.Shared.ADT.EMP;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpContainerProtectionComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, BatteryData> Batteries = new();

    [DataField]
    public string ContainerId = "cell_slot";

    [DataField]
    public float Multiplier = 0f;

    [DataField]
    public bool CapEnergyConsumption = false;

    [DataField]
    public bool CapRelativeToMaxCharge = true;

    [DataField]
    public float CapPercentage = 0.5f;

    public struct BatteryData
    {
        public float Protection;
        public bool CapConsumption;
        public bool CapRelativeToMax;
        public float CapPercentage;

        public BatteryData(float protection, bool cap, bool capType, float capPercentage)
        {
            Protection = protection;
            CapConsumption = cap;
            CapRelativeToMax = capType;
            CapPercentage = capPercentage;
        }
    }
}
