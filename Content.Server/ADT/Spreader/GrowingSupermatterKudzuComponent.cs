using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Spreader;

[RegisterComponent, Access(typeof(SupermatterKudzuSystem)), AutoGenerateComponentPause]
public sealed partial class GrowingSupermatterKudzuComponent : Component
{
    /// <summary>
    /// The next time kudzu will try to tick its growth level.
    /// </summary>
    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;
}
