using System.Numerics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Segmented;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTSegmentSteppingComponent : Component
{
    [ViewVariables]
    public EntityUid Parent;

    [ViewVariables]
    public Vector2 From;

    [ViewVariables]
    public Vector2 To;

    [ViewVariables]
    public int Group;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Start;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan End;
}
