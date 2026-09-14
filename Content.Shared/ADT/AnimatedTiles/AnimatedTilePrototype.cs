using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.AnimatedTiles;

[Prototype("animatedTile")]
public sealed partial class AnimatedTilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    [DataField]
    public Color Color = Color.White;
}
