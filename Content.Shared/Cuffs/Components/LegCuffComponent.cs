using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent]
public sealed partial class LegCuffComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier CuffedSprite = default!;

    [DataField(required: true)]
    public SoundSpecifier StartCuffSound = default!;

    [DataField(required: true)]
    public SoundSpecifier RemoveCuffSound = default!;

    [DataField(required: true)]
    public float ApplyDelay = 4f;
}
