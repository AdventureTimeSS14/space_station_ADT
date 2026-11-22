using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Clothing.Components, no shitmed fields

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DamageOverTimeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage { get; set; } = new();

    [DataField(customTypeSerializer: typeof(TimespanSerializer)), AutoNetworkedField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool IgnoreResistances { get; set; }

    [DataField, AutoNetworkedField]
    public float Multiplier = 1f;

    [DataField, AutoNetworkedField]
    public float MultiplierIncrease;

    [DataField, AutoPausedField]
    public TimeSpan NextTickTime = TimeSpan.Zero;
}
