using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class TeleportActComponent : Component
{
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("blinkVolume")]
    public float BlinkVolume = 5f;

    [DataField("blinkAction")]
    public EntProtoId Action = "CompActionTeleport";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
