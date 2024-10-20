using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Server.ADT.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechArmorComponent : Component
{
    /// <summary>
    /// damage modifiers to add
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;

    public DamageModifierSet? OriginalModifiers;
}
