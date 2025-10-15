using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomBreedOnSpawnComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<BreedPrototype>> Mutations = new();
}
