using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class ProjectileActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionShoot";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public EntProtoId Prototype = "BulletKinetic";

    [DataField]
    public float Speed = 20f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/Xeno/alien_spitacid.ogg", AudioParams.Default.WithVolume(5f));
}
