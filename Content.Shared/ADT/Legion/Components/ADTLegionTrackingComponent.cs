using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTLegionTrackingComponent : Component
{
    [ViewVariables]
    public readonly HashSet<EntityUid> Skulls = new();

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;
}
