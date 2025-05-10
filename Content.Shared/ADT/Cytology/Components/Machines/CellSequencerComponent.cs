using Content.Shared.ADT.Cytology.Components.Container;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Cytology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [ViewVariables]
    public List<Cell> Cells = [];

    [ViewVariables]
    public List<Entity<CellContainerComponent>> CellContainers = [];
}
