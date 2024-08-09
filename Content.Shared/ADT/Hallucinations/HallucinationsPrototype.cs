using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Hallucinations;

/// <summary>
///     Packs of entities that can become a hallucination
/// </summary>

[Prototype("hallucinationsPack")]
public sealed partial class HallucinationsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     List of prototypes that are spawned as a hallucination.
    /// </summary>
    [DataField("entities")]
    public List<EntProtoId> Entities = new();

    [DataField("spawnRange")]
    public float Range = 7f;

    [DataField("spawnRate")]
    public float SpawnRate = 15f;

    [DataField("minChance")]
    public float MinChance = 0.8f;

    [DataField("maxChance")]
    public float MaxChance = 0.8f;

    [DataField("increasedPerSpawn")]
    public float IncreaseChance = 0.1f;

    [DataField("maxSpawns")]
    public int MaxSpawns = 3;

    [DataField("epidemic")]
    public bool Epicemic = false;
}
