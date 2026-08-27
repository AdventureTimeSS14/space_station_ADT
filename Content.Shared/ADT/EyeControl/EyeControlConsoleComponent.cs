using Content.Shared.ADT.Areas;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EyeControl;

[RegisterComponent]
public sealed partial class EyeControlConsoleComponent : Component
{
    [DataField]
    public EntProtoId EyeProto = "ADTCameraEye";

    [DataField]
    public EntProtoId<AreaComponent>? Area;

    [DataField]
    public string? VisionNetwork;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public EntityUid? Pilot;

    [DataField]
    public EntityUid? Eye;
}
