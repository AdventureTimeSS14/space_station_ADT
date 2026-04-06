using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class DamagedByContactComponent : Component
{
    [DataField("nextSecond", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables]
    public DamageSpecifier? Damage;

    /// <summary>
    /// ADT-Tweak
    /// The specific entity that is dealing damage on contact.
    /// Used to properly remove this component when the source is deleted.
    /// </summary>
    [ViewVariables]
    public EntityUid Source = EntityUid.Invalid;
}
