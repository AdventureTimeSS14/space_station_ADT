using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class JumpActComponent : Component
{
    [DataField("jumpSound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Footsteps/suitstep2.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("jumpVolume")]
    public float Volume = 1f;

    [DataField("jumpStrength")]
    public float Strength = 13f;

    [DataField("jumpAction")]
    public EntProtoId Action = "CompActionJump";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
