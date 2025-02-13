using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Content.Shared.Damage;

namespace Content.Shared.ADT.Combat;

/// <summary>
///     This component gives an item an action that will equip or un-equip some clothing e.g. hardsuits and hardsuit helmets.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboComponent : Component
{
    [DataField]
    public List<CombatMove> AvailableMoves { get; private set; } = new List<CombatMove>();

    public List<CombatAction> CurrestActions { get; private set; } = new List<CombatAction>();
}

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

    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ComboDamage = default!;

    [DataField]
    public List<BaseComboEvent> ComboEvent = new List<BaseComboEvent>{};

    [DataField]
    public String? Emote = null;
}
