using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.VendingMachines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTClothingPaintComponent : Component
{
    public const string TrinketLayerPrefix = "trinkets";

    [DataField, AutoNetworkedField]
    public Color? PaintColor;
}