using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.AshFlora;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTAshFloraRegrowthComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public TimeSpan MinTime = TimeSpan.FromMinutes(8);

    [DataField]
    public TimeSpan MaxTime = TimeSpan.FromMinutes(16);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RegrowAt;
}
