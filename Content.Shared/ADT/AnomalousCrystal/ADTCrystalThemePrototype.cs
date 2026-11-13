using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AnomalousCrystal;

[Prototype("adtCrystalTheme")]
public sealed partial class ADTCrystalThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ProtoId<ContentTileDefinition>? Floor;

    [DataField]
    public EntProtoId? Wall;

    [DataField]
    public EntProtoId? Chair;

    [DataField]
    public EntProtoId? Table;

    [DataField]
    public List<EntProtoId> Flora = new();

    [DataField]
    public float FloraChance = 0.08f;
}
