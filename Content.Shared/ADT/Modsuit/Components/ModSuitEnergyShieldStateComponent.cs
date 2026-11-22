using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ModSuitEnergyShieldStateComponent : Component
{
    [DataField]
    public float MaxHealth = 15f;

    [DataField]
    public TimeSpan RechargeTime = TimeSpan.FromMinutes(5);

    [DataField]
    public float? Health;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextRechargeTime;
}
