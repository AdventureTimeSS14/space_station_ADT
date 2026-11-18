using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Guidebook;

[Prototype("adtGuidebookCategory")]
public sealed partial class ADTGuidebookCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [DataField(required: true)]
    public LocId Description = default!;

    [DataField(required: true)]
    public ResPath Icon;

    [DataField]
    public int Order;

    [DataField]
    public bool Rules;

    [DataField]
    public List<ProtoId<GuideEntryPrototype>> Entries = new();
}
