using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeFingerGunComponent : Component
{
    [DataField("fingerGunAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FingerGunAction = "ADTActionMimeFingerGun";

    [DataField("fingerGunActionEntity")]
    public EntityUid? FingerGunActionEntity;
}
