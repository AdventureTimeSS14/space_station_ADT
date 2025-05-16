using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Cytology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellMutagenicInjectorComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [ViewVariables]
    public List<Cell> Cells = [];
}
