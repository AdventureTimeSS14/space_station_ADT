using Content.Server.Destructible.Thresholds;
using Robust.Shared.Serialization;

namespace Content.Server.Destructible;

public sealed partial class DestructibleComponent : ISerializationHooks
{
    [DataField(customTypeSerializer: typeof(DamageThresholdsSerializer))]
    public List<DamageThreshold>? ThresholdsOverride;

    void ISerializationHooks.AfterDeserialization()
    {
        if (ThresholdsOverride != null)
            Thresholds = ThresholdsOverride;
    }
}
