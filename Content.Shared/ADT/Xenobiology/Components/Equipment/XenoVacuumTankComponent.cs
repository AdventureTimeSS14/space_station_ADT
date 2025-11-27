using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components.Equipment;

/// <summary>
/// This handles the tanks for xeno vacuums.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoVacuumTankComponent : Component
{
    /// <summary>
    /// The ID of the tank's container.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container StorageTank = new();

    /// <summary>
    /// The maximum amount of entities in this tank at a time.
    /// Will be upgradable.
    /// </summary>
    [DataField]
    public int MaxEntities = 1;

    /// <summary>
    /// The EntityUid of the nozzle attached to this tank.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? LinkedNozzle;
}
