using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Colormat;

[RegisterComponent]
public sealed partial class ADTColormatComponent : Component
{
    [DataField]
    public string SlotId = "colormatslot";
}
