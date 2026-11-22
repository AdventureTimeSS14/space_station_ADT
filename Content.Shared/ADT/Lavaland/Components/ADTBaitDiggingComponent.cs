using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTBaitDiggingComponent : Component
{
    [DataField]
    public ProtoId<ContentTileDefinition> SourceTile = "FloorBasalt";

    [DataField]
    public ProtoId<ContentTileDefinition> DugTile = "ADTFloorBasaltDug";

    [DataField]
    public float Chance = 0.3f;

    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public TimeSpan DigTime = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");

    [ViewVariables]
    public HashSet<Vector2i> DugTiles = new();
}
