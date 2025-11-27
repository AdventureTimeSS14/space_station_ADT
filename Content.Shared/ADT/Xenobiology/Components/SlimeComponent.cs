using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Components;

/// <summary>
/// Stores important information about slimes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeComponent : Component
{
    [DataField]
    public EntProtoId DefaultSlimeProto = "ADTMobXenoSlime";

    [DataField, AutoNetworkedField]
    public Color SlimeColor = Color.FromHex("#828282");

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<BreedPrototype> Breed = "GreyMutation";

    [DataField]
    public EntProtoId DefaultExtract = "GreySlimeExtract";

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<BreedPrototype>> PotentialMutations = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<BreedPrototype>> SpecialPotentialMutations = new();

    [DataField]
    public Container Stomach = new();

    [DataField]
    public int MaxContainedEntities = 1;

    [DataField]
    public TimeSpan OnRemovalStunDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan LatchDoAfterDuration = TimeSpan.FromSeconds(1);

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Tamer;

    [DataField]
    public EntProtoId TameEffect = "EffectHearts";

    public EntityUid? LatchedTarget;

    [DataField, AutoNetworkedField]
    public int MaxOffspring = 4;

    [DataField, AutoNetworkedField]
    public int ExtractsProduced = 1;

    [DataField, AutoNetworkedField]
    public float MutationChance = 0.45f;

    [DataField, AutoNetworkedField]
    public float SpecialMutationChance = 0.01f;

    [DataField, AutoNetworkedField]
    public float MitosisHunger = 200f;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;

    [DataField, AutoNetworkedField]
    public float JitterDifference = 25f;

    [DataField, AutoNetworkedField]
    public bool ShouldHaveShader;

    [DataField, AutoNetworkedField]
    public string? Shader;

    [DataField]
    public SoundPathSpecifier MitosisSound = new("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public SoundPathSpecifier EatSound = new("/Audio/Voice/Talk/slime.ogg");
}
