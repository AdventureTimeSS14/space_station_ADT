using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Combat;

[Serializable, NetSerializable]
public enum CombatAction
{
    Disarm,
    Grab,
    Hit
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
    public List<IComboEffect> ComboEvent = new List<IComboEffect>{};
}
