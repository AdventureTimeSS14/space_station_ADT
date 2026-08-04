using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Weapons.KineticCooldown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTKineticCooldownComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUse;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan LastUseStart;

    [DataField, AutoNetworkedField]
    public float RangedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float MeleeMultiplier = 1f;
}
