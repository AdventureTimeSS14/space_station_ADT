using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLegionSpawnerComponent : Component
{
    [DataField]
    public List<EntProtoId> Prototypes = new();

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(20);

    [DataField]
    public int MaxMobs = 3;

    [DataField]
    public LocId SpawnMessage = "adt-legion-spawner-peel";

    [DataField]
    public bool DeleteOnDeath;

    [ViewVariables]
    public TimeSpan NextSpawn;

    [ViewVariables]
    public List<EntityUid> Spawned = new();
}
