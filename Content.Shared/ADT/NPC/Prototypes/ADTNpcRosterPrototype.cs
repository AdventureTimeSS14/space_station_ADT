using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NPC.Prototypes;

[Prototype("adtNpcRoster")]
public sealed partial class ADTNpcRosterPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ADTNpcRosterEntry> Entries = new();
}

[DataDefinition]
public sealed partial class ADTNpcRosterEntry
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Sex Sex = Sex.Male;

    [DataField]
    public List<EntProtoId> Prototypes = new();
}
