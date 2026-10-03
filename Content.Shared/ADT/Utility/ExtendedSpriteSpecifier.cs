using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Utility;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ExtendedSpriteSpecifier
{
    [DataField("sprite")]
    public SpriteSpecifier Sprite { get; internal set; } = default!;

    [DataField("color")]
    public Color SpriteColor = Color.White;

    [DataField("scale")]
    public Vector2 SpriteScale = new(1, 1);

    [DataField("noRot")]
    public bool SpriteRotation = true;

    public ExtendedSpriteSpecifier(SpriteSpecifier sprite, Color? color = null, Vector2? scale = null, bool? rotation = null)
    {
        Sprite = sprite;
        SpriteColor = color ?? Color.White;
        SpriteScale = scale ?? new(1, 1);
        SpriteRotation = rotation ?? true;
    }
}
