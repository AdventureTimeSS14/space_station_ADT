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
    public bool Inserted = false;
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
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Slots = new();

    /// <summary>
    /// The components to add when activated.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// The components to remove when deactivated.
    /// If this is null <see cref="Components"/> is reused.
    /// </summary>
    [DataField]
    public ComponentRegistry? RemoveComponents;

    [AutoNetworkedField]
    public TimeSpan Ejecttick;
}

public enum ExamineColor
{
    Red,
    Yellow,
    Green
}
