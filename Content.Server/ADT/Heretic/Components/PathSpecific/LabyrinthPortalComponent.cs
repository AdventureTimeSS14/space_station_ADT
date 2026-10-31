//

using Robust.Shared.Prototypes;
using Content.Shared.Random;

namespace Content.Server.Heretic.Components.PathSpecific;

[RegisterComponent]
public sealed partial class LabyrinthPortalComponent : Component
{
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> ToSpawn = "LabyrinthPortalSpawnTable";

    [DataField]
    public float SpawnChance = 1f;

    [DataField]
    public float MinSpawnChance = 0.1f;

    [DataField]
    public float ChanceReduction = 0.1f;

    [DataField]
    public bool Paused;

    [DataField]
    public EntityUid? HereticMind;

    [DataField]
    public List<EntityUid> SpawnedMobs = new();

    [DataField]
    public int MaxMobs = 20;
}
