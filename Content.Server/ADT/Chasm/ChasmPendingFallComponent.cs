using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chasm;

/// <summary>
///     Added to entities with MindContainerComponent that are pending fall into a chasm after a delay.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChasmPendingFallComponent : Component
{
    [DataField("nextFallTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextFallTime = TimeSpan.Zero;

    public EntityUid ChasmUid;
}
