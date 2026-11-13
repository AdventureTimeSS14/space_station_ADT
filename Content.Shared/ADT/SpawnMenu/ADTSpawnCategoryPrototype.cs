using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SpawnMenu;

[Prototype("adtSpawnCategory")]
public sealed partial class ADTSpawnCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Name;

    [DataField]
    public ProtoId<ADTSpawnCategoryPrototype>? ParentCategory;

    [DataField]
    public ProtoId<ADTSpawnCategoryPrototype>? RuleFor;

    [DataField]
    public int Order;

    [DataField]
    public int Priority;

    [DataField]
    public bool Service;

    [DataField]
    public bool SplitBySuffix;

    [DataField]
    public List<string> Paths = new();

    [DataField]
    public List<string> ExcludePaths = new();

    [DataField]
    public List<string> Components = new();

    [DataField]
    public List<string> AnyComponents = new();

    [DataField]
    public List<string> ExcludeComponents = new();

    [DataField]
    public List<SlotFlags> ClothingSlots = new();

    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public List<string> Ancestors = new();

    [DataField]
    public List<string> IdPrefixes = new();

    [DataField]
    public List<string> SuffixTags = new();

    [DataField]
    public List<ProtoId<EntityCategoryPrototype>> EntityCategories = new();

    [DataField]
    public float? MinMeleeDamage;
}
