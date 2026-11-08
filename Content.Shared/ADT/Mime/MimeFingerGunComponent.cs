using Robust.Shared.Prototypes;
namespace Content.Shared.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeFingerGunComponent : Component
{
    [DataField("fingerGunAction")]
    public EntProtoId? FingerGunAction = "ADTActionMimeFingerGun";

    [DataField("fingerGunActionEntity")]
    public EntityUid? FingerGunActionEntity;

    [DataField("fingerGunEntity")]
    public EntityUid? FingerGunEntity;

    [DataField("fingerGunPrototype")]
    public string FingerGunPrototype = "ADTMimeFingerGun";
}
