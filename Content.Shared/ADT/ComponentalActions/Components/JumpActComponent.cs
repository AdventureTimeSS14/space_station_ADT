using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class JumpActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionJump";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Footsteps/suitstep2.ogg");

    [DataField]
    public float Strength = 13f;
}
