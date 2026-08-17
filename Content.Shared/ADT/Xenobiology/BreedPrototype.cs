using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology;

/// <summary>
/// This prototype stores information about different slime breeds.
/// </summary>
[Prototype("breed")]
public sealed partial class BreedPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = null!;

    /// <summary>
    /// Used to set the slime's name.
    /// </summary>
    [DataField(required: true)]
    public LocId BreedName = string.Empty;

    /// <summary>
    /// The extract produced when this breed is ground.
    /// </summary>
    [DataField]
    public EntProtoId ProducedExtract = "GreySlimeExtract";

    [DataField]
    public Color SlimeColor = Color.FromHex("#828282");

    [DataField]
    public int MaxOffspring = 4;

    [DataField]
    public float MutationChance = 0.45f;

    [DataField]
    public HashSet<ProtoId<BreedPrototype>> PotentialMutations = new();

    [DataField]
    public bool ShouldHaveShader;

    [DataField]
    public string? Shader;
}
