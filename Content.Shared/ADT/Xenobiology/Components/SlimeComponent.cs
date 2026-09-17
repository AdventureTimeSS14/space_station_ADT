using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Components;

/// <summary>
/// Stores important information about slimes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeComponent : Component
{
    /// <summary>
    /// Default slime.
    /// </summary>
    [DataField]
    public EntProtoId DefaultSlimeProto = "MobSlimeXenobioBaby";

    /// <summary>
    /// What color is the slime?
    /// </summary>
    [DataField]
    public Color SlimeColor = Color.FromHex("#FFFFFF");

    /// <summary>
    /// What is the current slime's current breed?
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BreedPrototype> Breed = "GreyMutation";

    /// <summary>
    /// If the associated breed prototype cannot be found,
    /// it will use this extract as a fallback.
    /// </summary>
    [DataField]
    public EntProtoId DefaultExtract = "GreySlimeExtract";

    /// <summary>
    /// If the mutation chance is met, what potential mutations are available?
    /// </summary>
    [DataField]
    public HashSet<ProtoId<BreedPrototype>> PotentialMutations = new();

    /// <summary>
    /// The stomach! Holds all consumed entities to be consumed.
    /// </summary>
    [DataField]
    public Container Stomach = new();

    /// <summary>
    /// How many entities the slime can digest at once.
    /// </summary>
    [DataField]
    public int MaxContainedEntities = 1;

    /// <summary>
    /// How long each entity is stunned for when removed from the stomach (Fuck you gus.)
    /// </summary>
    [DataField]
    public TimeSpan OnRemovalStunDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the do-after to start a latch is.
    /// </summary>
    [DataField]
    public TimeSpan LatchDoAfterDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The entity which has tamed this slime.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Tamer;

    [DataField]
    public EntProtoId TameEffect = "EffectHearts";

    /// <summary>
    /// The entity, if any, currently being consumed by the slime.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LatchedTarget;

    /// <summary>
    /// The maximum amount of offspring produced by mitosis.
    /// </summary>
    [DataField]
    public int MaxOffspring = 4;

    /// <summary>
    /// How many extracts will be produced by this slime?
    /// </summary>
    [DataField]
    public int ExtractsProduced = 1;

    /// <summary>
    /// What is the chance of offspring mutating? (this is per/offspring)
    /// </summary>
    [DataField]
    public float MutationChance = 0.45f;

    /// <summary>
    /// What hunger threshold must be met for mitosis?
    /// </summary>
    [DataField]
    public float MitosisHunger = 125f;

    /// <summary>
    /// How long in between each mitosis/breeding check?
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When is the next mitosis/breeding check?
    /// </summary>
    [DataField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// What should the minimum difference be between the current hunger and the mitosis hunger
    /// before the entity starts to shake?
    /// </summary>
    [DataField]
    public float JitterDifference = 25f;

    /// <summary>
    /// Should this slime have a shader?
    /// </summary>
    [DataField]
    public bool ShouldHaveShader;

    /// <summary>
    /// Which shader are we using?
    /// </summary>
    [DataField]
    public string? Shader;

    /// <summary>
    /// What sound should we play when mitosis occurs?
    /// </summary>
    [DataField]
    public SoundPathSpecifier MitosisSound = new("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// What sound should we play when the slime eats/latches.
    /// </summary>
    [DataField]
    public SoundPathSpecifier EatSound = new("/Audio/Voice/Talk/slime.ogg");

    [DataField]
    public float FriendSightRange = 10f;

    /// <summary>
    /// How much this slime likes its tamer. 0-1.
    /// Grows when the slime eats monkeys, shrinks when upset.
    /// </summary>
    [DataField]
    public float Friendship;

    /// <summary>
    /// How much friendship is gained per eaten monkey.
    /// </summary>
    [DataField]
    public float FriendshipPerMeal = 0.1f;

    /// <summary>
    /// How much friendship is required for the slime to follow commands.
    /// </summary>
    [DataField]
    public float MinFriendshipToCommand = 0.15f;

    /// <summary>
    /// How much friendship is lost when the slime refuses an order.
    /// </summary>
    [DataField]
    public float FriendshipLossOnRefusal = 0.1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? FollowingTarget;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextFollowUpdate;

    [DataField]
    public float ChaseSpeedMultiplier = 1.3f;

    [DataField]
    public TimeSpan StopDuration = TimeSpan.FromSeconds(10);
}
