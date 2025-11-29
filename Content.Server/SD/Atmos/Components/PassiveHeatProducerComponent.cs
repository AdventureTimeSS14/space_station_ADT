using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class PassiveHeatProducerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyPerSecond = 15000f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;
}
