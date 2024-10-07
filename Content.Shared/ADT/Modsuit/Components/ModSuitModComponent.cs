using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitModComponent : Component
{
    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Slot = "head";

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

    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedMod = -0.01f;

    public bool Inserted = false;
}
