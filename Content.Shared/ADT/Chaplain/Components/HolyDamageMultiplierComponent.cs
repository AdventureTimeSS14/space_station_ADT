using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Automaticaly multiplies damage from holy type of damage
/// </summary>
[RegisterComponent]
public sealed partial class HolyDamageMultiplierComponent : Component
{
    /// <summary>
    /// Multiplier for holy damage
    /// </summary>
    [DataField("multiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 2.0f; // По умолчанию удваиваем святой урон
}