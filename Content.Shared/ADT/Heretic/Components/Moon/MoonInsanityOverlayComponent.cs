using Content.Shared.ADT.Heretic.SpriteOverlay;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent]
public sealed partial class MoonInsanityOverlayComponent : BaseSpriteOverlayComponent
{
    public override Enum Key { get; set; } = MoonInsanityOverlayKey.Key;

    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new("ADT/Heretic/Effects/moon_insanity_overlay.rsi"), "icon");
}

public enum MoonInsanityOverlayKey : byte
{
    Key,
}