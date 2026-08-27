using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent]
public sealed partial class LegCuffComponent : Component
{
    [DataField]
    public SpriteSpecifier CuffedSprite =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Objects/Misc/legcuffs.rsi"), "leg-irons");

    [DataField]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/ADT/Entities/Objects/handcuffs.ogg");

    [DataField]
    public SoundSpecifier RemoveCuffSound = new SoundPathSpecifier("/Audio/ADT/Entities/Objects/handcuffs.ogg");

    [DataField]
    public float ApplyDelay = 4f;
}
