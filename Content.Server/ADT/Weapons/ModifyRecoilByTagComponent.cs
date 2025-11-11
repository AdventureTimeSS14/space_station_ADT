using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged;

[RegisterComponent]
public sealed partial class ModifyRecoilByTagComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<TagPrototype>, float> Modifiers = new();
}
