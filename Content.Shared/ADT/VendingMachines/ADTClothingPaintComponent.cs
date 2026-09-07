using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.VendingMachines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTClothingPaintComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color? PaintColor;

    [DataField, AutoNetworkedField]
    public string TrinketLayerPrefix = "trinkets";
}