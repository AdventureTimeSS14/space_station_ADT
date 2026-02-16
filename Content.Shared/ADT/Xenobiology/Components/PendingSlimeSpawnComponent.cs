using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PendingSlimeSpawnComponent : Component
{
    [DataField] public EntProtoId BasePrototype = "ADTMobXenoSlime";
    [DataField] public ProtoId<BreedPrototype> Breed = "GreyMutation";
}
