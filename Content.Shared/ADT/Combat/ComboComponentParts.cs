using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Combat;

[Serializable, NetSerializable]
public enum CombatAction
{
    Disarm,
    Grab,
    Hit,
    Crawl
}
[Serializable, NetSerializable]
public enum WeaponCombatAction
{
    ProtectiveHit,      // 0 (00)
    OffensiveHit,       // 1 (01)
    ProtectiveWideHit,  // 2 (10)
    OffensiveWideHit    // 3 (11)
}


[Serializable, NetSerializable]
public enum ComboWeaponStand : sbyte
{
    Protective,
    Offensive
}


[Serializable, NetSerializable]
public enum ComboWeaponState : sbyte
{
    State,
}

[DataDefinition]
public sealed partial class CombatMove
{
    /// <summary>
    /// это надо указывать в прототипе в виде списка
    /// </summary>
    [DataField]
    public List<CombatAction> ActionsNeeds { get; private set; } = new List<CombatAction>();

    [DataField]
    public List<IComboEffect> ComboEvent = new List<IComboEffect> { };
}

[DataDefinition]
public sealed partial class ComboWeaponMove
{
    /// <summary>
    /// это надо указывать в прототипе в виде списка
    /// </summary>
    [DataField]
    public List<WeaponCombatAction> ActionsNeeds { get; private set; } = new List<WeaponCombatAction>();

    [DataField]
    public List<IComboEffect> ComboEvent = new List<IComboEffect> { };
}
