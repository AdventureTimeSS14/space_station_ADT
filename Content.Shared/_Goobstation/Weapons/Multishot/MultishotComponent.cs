// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Common.Weapons.Multishot;

/// <summary>
/// This component allows guns to be shot with another such gun at the same time by holding them in both hands.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultishotComponent : Component
{
    /// <summary>
    /// Shows that this entity is affected with multishot debuffs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MultishotAffected;

    /// <summary>
    /// Spread multiplier of the gun while dual-wielding.
    /// </summary>
    [DataField]
    public float SpreadMultiplier = 1.5f;

    /// <summary>
    /// Flat spread increase of the gun while dual-wielding.
    /// </summary>
    [DataField]
    public float SpreadAddition = 5f;

    /// <summary>
    ///  Stamina damage applied to the entity when firing this gun with another one.
    /// </summary>
    [DataField]
    public float StaminaDamage;

    [DataField]
    public string ExamineMessage = "multishot-component-examine";
}
