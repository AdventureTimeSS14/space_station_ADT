using Content.Shared.ADT.Areas;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyControlConsoleComponent : Component
{
    [DataField]
    public EntProtoId EyeProto = "ADTXenobiologyEye";

    [DataField]
    public EntProtoId<AreaComponent> Area = "AreaXenobio";

    [DataField]
    public int MaxSlimeCapacity = 5;

    [DataField]
    public float InteractRange = 0.1f;

    [DataField]
    public EntityUid? Pilot;

    [DataField]
    public EntityUid? Eye;

    [DataField]
    public SoundSpecifier? SuctionSound = new SoundPathSpecifier("/Audio/Effects/zzzt.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Effects/trashbag3.ogg");

    public const string SlimeContainerId = "xenobio_control_slimes";
}
