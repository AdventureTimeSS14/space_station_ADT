using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Bones;

[RegisterComponent]
public sealed partial class ADTFractureSourceComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, float> ZoneWeights = new()
    {
        ["HandLeft"] = 3f,
        ["HandRight"] = 3f,
        ["FootLeft"] = 3f,
        ["FootRight"] = 3f,
        ["ArmLeft"] = 3f,
        ["ArmRight"] = 3f,
        ["LegLeft"] = 3f,
        ["LegRight"] = 3f,
        ["Head"] = 1f,
        ["Torso"] = 1f,
    };

    [DataField]
    public float DamageMultiplier = 1f;
}
