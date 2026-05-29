using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeSilenceComponent : Component
{
    [DataField("silenceAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SilenceAction = "ADTActionMimeSilence";

    [DataField("silenceActionEntity")]
    public EntityUid? SilenceActionEntity;
}
