using Content.Shared.ADT.Traits.Effects;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

public sealed partial class TraitPrototype
{
    public ProtoId<TraitCategoryPrototype> ADTCategory => Category ?? TraitCategoryPrototype.Default;

    [DataField]
    public List<BaseTraitEffect> Effects = new();

    [DataField]
    public List<ProtoId<TraitPrototype>> Conflicts = new();

    [DataField]
    public HashSet<JobRequirement>? Requirements;

    [DataField]
    public List<ProtoId<SpeciesPrototype>> SpeciesBlacklist = new();

    [DataField]
    public List<ProtoId<SpeciesPrototype>> SpeciesWhitelist = new();

    [DataField]
    public List<ProtoId<JobPrototype>> JobBlacklist = new();

    [DataField]
    public List<ProtoId<JobPrototype>> JobWhitelist = new();

    [DataField]
    public List<ProtoId<DepartmentPrototype>> DepartmentBlacklist = new();

    [DataField]
    public List<ProtoId<DepartmentPrototype>> DepartmentWhitelist = new();

    [DataField]
    public bool Quirk = false;

    [DataField]
    public bool SponsorOnly = false;

    [DataField]
    public bool RewriteComponents = false;
}
