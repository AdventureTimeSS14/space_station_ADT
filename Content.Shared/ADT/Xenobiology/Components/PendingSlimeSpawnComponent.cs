using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PendingSlimeSpawnComponent : Component
{
    [DataField] public EntProtoId BasePrototype = "MobSlimeXenobioBaby";
    [DataField] public ProtoId<BreedPrototype> Breed = "GreyMutation";
    [DataField] public EntityUid? SpawnedSlime;
}
