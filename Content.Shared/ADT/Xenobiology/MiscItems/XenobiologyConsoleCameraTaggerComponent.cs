using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.MiscItems;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenobiologyConsoleCameraTaggerComponent : Component
{
    [DataField]
    public string VisionNetwork = "AreaXenobio";
}
