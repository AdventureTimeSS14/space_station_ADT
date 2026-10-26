using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HealOnHoldingSystem))]
public sealed partial class HealOnHoldingComponent : Component
{
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Interval = 1f;

    [DataField("nextHeal", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextHeal = TimeSpan.Zero;
}

