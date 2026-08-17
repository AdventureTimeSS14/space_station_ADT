using System.Numerics;
using Content.Shared.ADT.Areas;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Procedural;

[Prototype("adtDungeonRoom")]
public sealed partial class ADTDungeonRoomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField(required: true)]
    public Vector2i Size;

    [DataField]
    public Dictionary<string, ADTDungeonRoomTile> Legend = new();

    [DataField]
    public List<string> Tiles = new();

    [DataField]
    public Dictionary<string, EntProtoId<AreaComponent>> AreaLegend = new();

    [DataField]
    public List<string> Areas = new();

    [DataField]
    public List<ADTDungeonRoomEntities> Entities = new();

    [DataField]
    public List<ADTDungeonRoomDecals> Decals = new();
}

[DataDefinition]
public sealed partial class ADTDungeonRoomTile
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField]
    public byte Variant;

    [DataField]
    public byte Rotation;
}

[DataDefinition]
public sealed partial class ADTDungeonRoomEntities
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField("rot")]
    public Angle Rotation = Angle.Zero;

    [DataField]
    public bool? Anchored;

    [DataField]
    public ADTComponentOverrides? Components;

    [DataField(required: true)]
    public List<Vector2> Positions = new();
}

[DataDefinition]
public sealed partial class ADTDungeonRoomDecals
{
    [DataField(required: true)]
    public ProtoId<DecalPrototype> Id;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public Angle Angle = Angle.Zero;

    [DataField]
    public int ZIndex;

    [DataField]
    public bool Cleanable;

    [DataField(required: true)]
    public List<Vector2> Positions = new();
}
