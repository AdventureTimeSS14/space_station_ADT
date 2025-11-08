using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server._SD.Implants;

[RegisterComponent]
public sealed partial class TeleportOnTriggerComponent : Component
{

    [DataField]
    public EntProtoId MarkerPrototype = "SD_LifelineMarker";

    [DataField("killOnTeleport")]
    public bool KillOnTeleport = true;
}
