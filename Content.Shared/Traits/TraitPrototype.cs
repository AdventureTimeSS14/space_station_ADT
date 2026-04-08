<<<<<<< HEAD
using Content.Shared.ADT.Traits.Effects;
using Content.Shared.Humanoid.Prototypes;
=======
>>>>>>> upstreamwiz/master
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
/// Describes a trait.
/// </summary>
[Prototype]
public sealed partial class TraitPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this trait.
    /// </summary>
    [DataField(required: true)] // ADT-Tweak new Traits
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    /// The description of this trait.
    /// </summary>
    [DataField(required: true)] // ADT-Tweak new Traits
    public LocId? Description { get; private set; }

    // ADT-Tweak start new Traits
    /// <summary>
    /// The category this trait belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TraitCategoryPrototype> Category;

    /// <summary>
    /// How many trait points this trait costs (positive) or grants (negative).
    /// </summary>
    [DataField]
    public int Cost = 1;
    // ADT-Tweak end new Traits

    /// <summary>
    /// Don't apply this trait to entities this whitelist IS NOT valid for.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Don't apply this trait to entities this whitelist IS valid for. (hence, a blacklist)
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The components that get added to the player, when they pick this trait.
<<<<<<< HEAD
    /// Legacy system - use Effects instead.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; private set; } = new(); // ADT-Tweak new Traits
=======
    /// NOTE: When implementing a new trait, it's preferable to add it as a status effect instead if possible.
    /// </summary>
    [DataField]
    [Obsolete("Use JobSpecial instead.")]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Special effects applied to the player who takes this Trait.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<JobSpecial> Specials { get; private set; } = new();
>>>>>>> upstreamwiz/master

    /// <summary>
    /// Gear that is given to the player, when they pick this trait.
    /// Legacy system - use SpawnItemInHandEffect instead.
    /// </summary>
    [DataField]
    public EntProtoId? TraitGear;

    /// <summary>
    /// Effects to apply to the entity when this trait is selected.
    /// Effects are applied in order.
    /// </summary>
    [DataField]
    public List<BaseTraitEffect> Effects = new(); // ADT-Tweak new Traits

    /// <summary>
    /// Other traits that are mutually exclusive with this one.
    /// </summary>
    [DataField]
    public List<ProtoId<TraitPrototype>> Conflicts = new(); // ADT-Tweak new Traits

    /// <summary>
    /// Conditions that must be met for this trait to be selectable and applied.
    /// All conditions must pass for the trait to be valid.
    /// </summary>
    [DataField]
    public HashSet<JobRequirement>? Requirements; // ADT-Tweak new Traits

    //ADT-Tweak-Start
    /// <summary>
    /// Will not be selectable if current species equals any of these
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>> SpeciesBlacklist = new();

    /// <summary>
    /// Will ONLY be selectable if current species equals any of these.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>> SpeciesWhitelist = new();

    /// <summary>
    /// Will not be selectable if current job equals any of these.
    /// Checked during spawn when job is assigned.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> JobBlacklist = new();

    /// <summary>
    /// Will ONLY be selectable if current job equals any of these.
    /// Checked during spawn when job is assigned.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> JobWhitelist = new();

    /// <summary>
    /// Will not be selectable if current job's department equals any of these.
    /// Checked during spawn when job is assigned.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> DepartmentBlacklist = new();

    /// <summary>
    /// Will ONLY be selectable if current job's department equals any of these.
    /// Checked during spawn when job is assigned.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> DepartmentWhitelist = new();

    [DataField]
    public bool Quirk = false;

    [DataField]
    public bool SponsorOnly = false;

    /// <summary>
    /// if true will rewrite components.
    /// </summary>
    [DataField]
    public bool RewriteComponents = false;
    //ADT-Tweak-End
}
