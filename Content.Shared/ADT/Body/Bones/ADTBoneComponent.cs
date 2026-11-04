using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Bones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTBoneComponent : Component
{
    [DataField]
    public float MinBrokenDamage = 30f;

    [DataField]
    public bool CanBreak = true;

    [DataField]
    public bool CanSplint;

    [DataField, AutoNetworkedField]
    public ProtoId<ADTFractureTypePrototype>? Fracture;

    [DataField, AutoNetworkedField]
    public bool Splinted;

    [DataField]
    public float SplintDistance;

    [DataField]
    public float LockedDamage;
}
