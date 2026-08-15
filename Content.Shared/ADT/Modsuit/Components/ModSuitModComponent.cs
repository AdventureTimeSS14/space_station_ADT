using Robust.Shared.Audio;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitModComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsInstantlyActive = false;

    [AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    ///     Module  limit
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Complexity = 1;

    /// <summary>
    ///     energy using
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyUsing = 0;

    /// <summary>
    /// The components to add when activated.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, ComponentRegistry> Components = new();

    /// <summary>
    /// The components to remove when deactivated.
    /// If this is null <see cref="Components"/> is reused.
    /// </summary>
    [DataField]
    public Dictionary<string, ComponentRegistry>? RemoveComponents;

    /// <summary>
    /// If true, the module can be toggled on/off at any time (<see cref="Active"/>), but its components are only
    /// applied while it is on AND the suit is fully equipped (all parts deployed).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresFullEquip = false;

    /// <summary>
    /// Server-side tracking of whether <see cref="Components"/> are currently applied. For a
    /// <see cref="RequiresFullEquip"/> module this trails <c>Active AND fully equipped</c>.
    /// </summary>
    public bool ComponentsApplied = false;
}

public enum ExamineColor
{
    Red,
    Yellow,
    Green
}
