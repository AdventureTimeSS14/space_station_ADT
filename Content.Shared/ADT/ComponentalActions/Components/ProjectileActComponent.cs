using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ProjectileActComponent : Component
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = "BulletKinetic";

    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")]
    public ComponentalActionsSpawnData Pos = new TargetCasterPos();

    [DataField]
    public SoundSpecifier ShootSound = new SoundPathSpecifier("/Audio/Weapons/Xeno/alien_spitacid.ogg");

    [DataField("shootVolume")]
    public float ShootVolume = 5f;

    [DataField("projAction")]
    public EntProtoId Action = "CompActionShoot";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
