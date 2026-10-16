using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Cloning;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTHeraMurderExposedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextBurst;
}
