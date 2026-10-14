using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Segmented.Components;

[RegisterComponent]
public sealed partial class ADTSegmentTetherComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public bool Unshaded;

    [DataField]
    public Vector2 SegmentOffset;

    [DataField]
    public Vector2 LeaderOffset;
}
