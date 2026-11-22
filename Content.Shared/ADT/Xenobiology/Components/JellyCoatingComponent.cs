using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class JellyCoatingComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.3f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(15);
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class SpeedBoostedComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.3f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;
}
