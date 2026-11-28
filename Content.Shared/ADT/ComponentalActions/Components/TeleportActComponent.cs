using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class TeleportActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionTeleport";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/blink.ogg", AudioParams.Default.WithVolume(5f));
}
