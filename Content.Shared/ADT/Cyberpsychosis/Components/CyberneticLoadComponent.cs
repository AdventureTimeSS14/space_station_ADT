using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Cyberpsychosis;

[RegisterComponent]
public sealed partial class CyberneticLoadComponent : Component
{
    [DataField]
    public int ImplantLoad = 25;
}
