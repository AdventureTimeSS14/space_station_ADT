using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.ZombieJump;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedZombieJumpSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class ZombieJumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionZombieJump";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public float JumpDistance = 5f;

    [DataField, AutoNetworkedField]
    public float JumpThrowSpeed = 15f;

    [DataField, AutoNetworkedField]
    public TimeSpan CollideKnockdown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? JumpSound;

    [DataField, AutoNetworkedField]
    public LocId? JumpFailedPopup = "jump-ability-failure";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextJumpTime;
}
public sealed partial class ZombieJumpEvent : InstantActionEvent;
