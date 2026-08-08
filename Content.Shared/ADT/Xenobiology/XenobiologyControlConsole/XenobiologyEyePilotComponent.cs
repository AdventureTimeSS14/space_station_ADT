using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyEyePilotComponent : Component
{
    [DataField(required: true)]
    public EntityUid Console;

    [DataField(required: true)]
    public EntityUid Eye;

    [DataField]
    public Dictionary<EntProtoId, EntityUid?> Actions = new();
}
