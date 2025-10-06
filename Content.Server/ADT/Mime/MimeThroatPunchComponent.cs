using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeThroatPunchComponent : Component
{
    [DataField("throatPunchAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ThroatPunchAction = "ADTActionMimeThroatPunch";

    [DataField("throatPunchActionEntity")]
    public EntityUid? ThroatPunchActionEntity;

    [DataField("muteDuration")]
    public float MuteDuration = 10f;
}
